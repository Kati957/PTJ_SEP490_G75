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
            // 1️⃣ Lưu bài đăng tuyển
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

            // 2️⃣ Chuẩn hoá text để embedding
            string text = $"{dto.Title}. {dto.Description}. Yêu cầu: {dto.Requirements}. Địa điểm: {dto.Location}. Lương: {dto.Salary}";
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3️⃣ Gọi OpenAI tạo embedding
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 4️⃣ Ghi log embedding
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

            // 5️⃣ Upsert lên Pinecone
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

            // 6️⃣ Query tương tự ở job_seeker_posts
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 20);

            var allCandidates = new List<(dynamic Seeker, double Score)>();

            foreach (var m in matches)
            {
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
                        x.CategoryId,
                        SeekerName = x.User.Username
                    })
                    .FirstOrDefaultAsync();

                if (seeker == null) continue;

                double hybridScore = ComputeHybridScore(
                    embeddingScore: m.Score,
                    employerLocation: dto.Location ?? "",
                    employerCategoryId: dto.CategoryID,
                    employerTitle: dto.Title ?? "",
                    seekerLocation: seeker.PreferredLocation,
                    seekerCategoryId: seeker.CategoryId,
                    seekerTitle: seeker.Title
                );

                allCandidates.Add((seeker, hybridScore));
            }

            // 7️⃣ Ưu tiên lọc theo địa điểm (VD: “Hà Nội”)
            var normalizedLocation = (dto.Location ?? "").ToLower();

            var localCandidates = allCandidates
                .Where(c => !string.IsNullOrEmpty(c.Seeker.PreferredLocation) &&
                            c.Seeker.PreferredLocation.ToLower().Contains(normalizedLocation))
                .OrderByDescending(c => c.Score)
                .ToList();

            // Nếu không có ai cùng khu vực → fallback toàn bộ
            var finalList = localCandidates.Any()
                ? localCandidates
                : allCandidates.OrderByDescending(c => c.Score).ToList();

            // 8️⃣ Ghi DB + trả kết quả
            var suggestions = new List<AIResultDto>();
            foreach (var (seeker, hybridScore) in finalList.Take(5))
            {
                _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                {
                    SourceType = "EmployerPost",
                    SourceId = post.EmployerPostId,
                    TargetType = "JobSeekerPost",
                    TargetId = seeker.JobSeekerPostId,
                    RawScore = hybridScore,
                    MatchPercent = (int)Math.Round(hybridScore * 100),
                    Reason = $"AI + Ưu tiên địa điểm gần '{dto.Location}'",
                    CreatedAt = DateTime.Now
                });

                suggestions.Add(new AIResultDto
                {
                    Id = $"JobSeekerPost:{seeker.JobSeekerPostId}",
                    Score = Math.Round(hybridScore * 100, 2),
                    ExtraInfo = seeker
                });
            }

            await _db.SaveChangesAsync();

            return new EmployerPostResultDto
            {
                Post = post,
                SuggestedCandidates = suggestions
            };
        }

        // 🧮 Hàm tính điểm hybrid tổng hợp
        private double ComputeHybridScore(
            double embeddingScore,
            string employerLocation,
            int? employerCategoryId,
            string employerTitle,
            string? seekerLocation,
            int? seekerCategoryId,
            string? seekerTitle)
        {
            double locationBonus = 0;
            double categoryBonus = 0;
            double titleBonus = 0;

            // 1️⃣ Ưu tiên địa điểm
            if (!string.IsNullOrEmpty(seekerLocation) && !string.IsNullOrEmpty(employerLocation))
            {
                var eLoc = employerLocation.ToLower();
                var sLoc = seekerLocation.ToLower();

                if (sLoc == eLoc)
                    locationBonus = 0.35; // cùng khu vực
                else if (eLoc.Contains(sLoc) || sLoc.Contains(eLoc))
                    locationBonus = 0.25; // gần khu vực
                else if (eLoc.Split(' ').Any(w => sLoc.Contains(w)))
                    locationBonus = 0.15; // có từ địa danh trùng
            }

            // 2️⃣ Ưu tiên loại công việc
            if (employerCategoryId.HasValue && seekerCategoryId.HasValue)
            {
                if (employerCategoryId == seekerCategoryId)
                    categoryBonus = 0.20;
            }

            // 3️⃣ Ưu tiên tiêu đề
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var eTitle = employerTitle.ToLower();
                var sTitle = seekerTitle.ToLower();

                if (sTitle.Contains(eTitle) || eTitle.Contains(sTitle))
                    titleBonus = 0.15;
            }

            // 4️⃣ Tổng điểm hybrid
            double hybrid = embeddingScore + locationBonus + categoryBonus + titleBonus;
            if (hybrid > 1) hybrid = 1;
            return hybrid;
        }

        // ======================================
        // 🧾 CÁC API LẤY DỮ LIỆU
        // ======================================

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
