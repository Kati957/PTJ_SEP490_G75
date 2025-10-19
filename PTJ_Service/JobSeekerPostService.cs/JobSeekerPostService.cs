using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Security.Cryptography;
using System.Text;
using JobSeekerPostModel = PTJ_Models.Models.JobSeekerPost;

namespace PTJ_Service.JobSeekerPostService
{
    public class JobSeekerPostService : IJobSeekerPostService
    {
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;

        public JobSeekerPostService(JobMatchingDbContext db, IAIService ai)
        {
            _db = db;
            _ai = ai;
        }

        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto)
        {
            // 1) Lưu post
            var post = new JobSeekerPostModel
            {
                UserId = dto.UserID,
                Title = dto.Title,
                Description = dto.Description,
                Age = dto.Age,
                Gender = dto.Gender,
                PreferredWorkHours = dto.PreferredWorkHours,
                PreferredLocation = dto.PreferredLocation,
                CategoryId = dto.CategoryID,
                PhoneContact = dto.PhoneContact,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
            };
            _db.JobSeekerPosts.Add(post);
            await _db.SaveChangesAsync();

            // 2) Chuẩn hoá text
            string text = $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}. Khu vực: {dto.PreferredLocation}.";
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3) Tạo embedding
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 3.1) Ghi AI_EmbeddingStatus
            _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
            {
                EntityType = "JobSeekerPost",
                EntityId = post.JobSeekerPostId,
                ContentHash = hash,
                Model = "text-embedding-3-large",
                VectorDim = vector.Length,
                PineconeId = $"JobSeekerPost:{post.JobSeekerPostId}",
                Status = "OK",
                UpdatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // 4) Upsert Pinecone
            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title ?? "",
                    location = dto.PreferredLocation ?? "",
                    postId = post.JobSeekerPostId
                });

            // 5) Query tương tự trong employer_posts
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 5);
            var suggestions = new List<AIResultDto>();

            if (matches.Any())
            {
                foreach (var m in matches)
                {
                    int empPostId = 0;
                    if (m.Id.StartsWith("EmployerPost:"))
                        int.TryParse(m.Id.Split(':')[1], out empPostId);

                    var job = await _db.EmployerPosts
                        .Include(x => x.User)
                        .Where(x => x.EmployerPostId == empPostId)
                        .Select(x => new
                        {
                            x.EmployerPostId,
                            x.Title,
                            x.Location,
                            x.WorkHours,
                            EmployerName = x.User.Username
                        })
                        .FirstOrDefaultAsync();

                    // Log AI_MatchSuggestions
                    if (empPostId > 0)
                    {
                        _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                        {
                            SourceType = "JobSeekerPost",
                            SourceId = post.JobSeekerPostId,
                            TargetType = "EmployerPost",
                            TargetId = empPostId,
                            RawScore = m.Score,
                            MatchPercent = (int)Math.Round(m.Score * 100),
                            Reason = "Gợi ý công việc từ AI theo embedding",
                            CreatedAt = DateTime.Now
                        });
                    }

                    suggestions.Add(new AIResultDto
                    {
                        Id = m.Id,
                        Score = Math.Round(m.Score * 100, 2),
                        ExtraInfo = job
                    });
                }

                await _db.SaveChangesAsync();
            }
            else
            {
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                {
                    EntityType = "JobSeekerPost",
                    EntityId = post.JobSeekerPostId,
                    Lang = "vi",
                    CanonicalText = text,
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return new JobSeekerPostResultDto
            {
                Post = post,
                SuggestedJobs = suggestions
            };
        }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync()
        {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = p.JobSeekerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Age = p.Age,
                    Gender = p.Gender,
                    PreferredWorkHours = p.PreferredWorkHours,
                    PreferredLocation = p.PreferredLocation,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    SeekerName = p.User.Username,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .ToListAsync();
        }

        public async Task<JobSeekerPostDtoOut?> GetByIdAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.JobSeekerPostId == id)
                .Select(p => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = p.JobSeekerPostId,
                    Title = p.Title,
                    Description = p.Description,
                    Age = p.Age,
                    Gender = p.Gender,
                    PreferredWorkHours = p.PreferredWorkHours,
                    PreferredLocation = p.PreferredLocation,
                    PhoneContact = p.PhoneContact,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    SeekerName = p.User.Username,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _db.JobSeekerPosts.FindAsync(id);
            if (post == null) return false;

            _db.JobSeekerPosts.Remove(post);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
