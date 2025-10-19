using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Security.Cryptography;
using System.Text;
using EmployerPostModel = PTJ_Models.Models.EmployerPost;

namespace PTJ_Service.EmployerPostService
{
    public class EmployerPostService : IEmployerPostService
    {
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;

        public EmployerPostService(JobMatchingDbContext db, IAIService ai)
        {
            _db = db;
            _ai = ai;
        }

        public async Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostDto dto)
        {
            // 1) Lưu post
            var post = new EmployerPostModel
            {
                UserId = dto.UserID,
                Title = dto.Title,
                Description = dto.Description,
                Salary = dto.Salary,
                Requirements = dto.Requirements,
                WorkHours = dto.WorkHours,
                Location = dto.Location,
                CategoryId = dto.CategoryID,
                PhoneContact = dto.PhoneContact,
                CreatedAt = DateTime.Now,
                Status = "Active"
            };
            _db.EmployerPosts.Add(post);
            await _db.SaveChangesAsync();

            // 2) Chuẩn hoá text
            string text = $"{dto.Title}. {dto.Description}. Yêu cầu: {dto.Requirements}. Địa điểm: {dto.Location}. Lương: {dto.Salary}";
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3) Tạo embedding
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 3.1) Ghi AI_EmbeddingStatus
            _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
            {
                EntityType = "EmployerPost",
                EntityId = post.EmployerPostId,
                ContentHash = hash,
                Model = "text-embedding-3-large",
                VectorDim = vector.Length,
                PineconeId = $"EmployerPost:{post.EmployerPostId}",
                Status = "OK",
                UpdatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // 4) Upsert Pinecone
            await _ai.UpsertVectorAsync(
                ns: "employer_posts",
                id: $"EmployerPost:{post.EmployerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title ?? "",
                    location = dto.Location ?? "",
                    salary = dto.Salary ?? 0,
                    postId = post.EmployerPostId
                });

            // 5) Query tương tự ở namespace job seeker
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 5);

            var suggestions = new List<AIResultDto>();

            if (matches.Any())
            {
                // Lấy thông tin seeker từ DB để render ra ngoài & lưu AI_MatchSuggestions
                foreach (var m in matches)
                {
                    // Id dạng "JobSeekerPost:123"
                    int seekerPostId = 0;
                    if (m.Id.StartsWith("JobSeekerPost:"))
                        int.TryParse(m.Id.Split(':')[1], out seekerPostId);

                    var seeker = await _db.JobSeekerPosts
                        .Include(x => x.User)
                        .Where(x => x.JobSeekerPostId == seekerPostId)
                        .Select(x => new
                        {
                            x.JobSeekerPostId,
                            x.Title,
                            x.PreferredLocation,
                            x.PreferredWorkHours,
                            SeekerName = x.User.Username
                        })
                        .FirstOrDefaultAsync();

                    // Log AI_MatchSuggestions
                    if (seekerPostId > 0)
                    {
                        _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                        {
                            SourceType = "EmployerPost",
                            SourceId = post.EmployerPostId,
                            TargetType = "JobSeekerPost",
                            TargetId = seekerPostId,
                            RawScore = m.Score,
                            MatchPercent = (int)Math.Round(m.Score * 100),
                            Reason = "Gợi ý từ AI theo độ tương đồng embedding",
                            CreatedAt = DateTime.Now
                        });
                    }

                    suggestions.Add(new AIResultDto
                    {
                        Id = m.Id,
                        Score = Math.Round(m.Score * 100, 2),
                        ExtraInfo = seeker
                    });
                }

                await _db.SaveChangesAsync();
            }
            else
            {
                // Không có match → lưu AI_ContentForEmbedding để xử lý sau
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                {
                    EntityType = "EmployerPost",
                    EntityId = post.EmployerPostId,
                    Lang = "vi",
                    CanonicalText = text,
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return new EmployerPostResultDto
            {
                Post = post,
                SuggestedCandidates = suggestions
            };
        }

        // LIST, BY ID, BY USER (như bạn đã có) – trả EmployerPostDtoOut
        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
        {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new EmployerPostDtoOut
                {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,
                    Location = p.Location,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    EmployerName = p.User.Username,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId)
        {
            return await _db.EmployerPosts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new EmployerPostDtoOut
                {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,
                    Location = p.Location,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    EmployerName = p.User.Username,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .ToListAsync();
        }

        public async Task<EmployerPostDtoOut?> GetByIdAsync(int id)
        {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.EmployerPostId == id)
                .Select(p => new EmployerPostDtoOut
                {
                    EmployerPostId = p.EmployerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Salary = p.Salary,
                    Requirements = p.Requirements,
                    WorkHours = p.WorkHours,
                    Location = p.Location,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    EmployerName = p.User.Username,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _db.EmployerPosts.FindAsync(id);
            if (post == null) return false;

            _db.EmployerPosts.Remove(post);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
