using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Globalization;
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

            // 3️⃣ Tạo embedding
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

            // 5️⃣ Upsert Pinecone
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

            // 6️⃣ Lấy danh sách ứng viên tương tự (job_seeker_posts)
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

                // ❌ Chỉ lấy ứng viên cùng Category
                if (dto.CategoryID != seeker.CategoryId) continue;

                double hybridScore = ComputeHybridScore(
                    m.Score,
                    dto.Location ?? "",
                    dto.CategoryID,
                    dto.Title ?? "",
                    seeker.PreferredLocation,
                    seeker.CategoryId,
                    seeker.Title
                );

                allCandidates.Add((seeker, hybridScore));
            }

            // 7️⃣ Ưu tiên ứng viên cùng khu vực
            var normalizedLocation = NormalizeString(dto.Location ?? "");
            var localCandidates = allCandidates
                .Where(c =>
                {
                    var loc = NormalizeString(c.Seeker.PreferredLocation ?? "");
                    return !string.IsNullOrEmpty(loc) &&
                           (loc.Contains(normalizedLocation) || normalizedLocation.Contains(loc));
                })
                .OrderByDescending(c => c.Score)
                .ToList();

            // Nếu không có ai cùng khu vực → fallback toàn bộ (vì cùng Category)
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
                    Reason = $"AI gợi ý ứng viên cùng loại Category và ưu tiên khu vực gần '{dto.Location}'",
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

        // 🧮 Tính điểm hybrid
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
            double categoryBonus = 0.2; // cùng Category luôn được +0.2
            double titleBonus = 0;
            double penalty = 1.0;

            var eLoc = NormalizeString(employerLocation);
            var sLoc = NormalizeString(seekerLocation ?? "");

            // Ưu tiên địa điểm
            if (!string.IsNullOrEmpty(eLoc) && !string.IsNullOrEmpty(sLoc))
            {
                if (eLoc == sLoc)
                    locationBonus = 0.35;
                else if (eLoc.Contains(sLoc) || sLoc.Contains(eLoc))
                    locationBonus = 0.25;
                else if (eLoc.Split(' ').Any(w => sLoc.Contains(w)))
                    locationBonus = 0.15;
                else
                    penalty = 0.8; // khác khu vực => giảm 20%
            }

            // Ưu tiên tiêu đề
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var eTitle = employerTitle.ToLowerInvariant();
                var sTitle = seekerTitle.ToLowerInvariant();
                if (sTitle.Contains(eTitle) || eTitle.Contains(sTitle))
                    titleBonus = 0.15;
            }

            double hybrid = (embeddingScore + locationBonus + categoryBonus + titleBonus) * penalty;
            if (hybrid > 1) hybrid = 1;
            return hybrid;
        }

        // 🔣 Bỏ dấu tiếng Việt để so khớp địa điểm linh hoạt
        private string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.ToLowerInvariant();
            input = input.Normalize(NormalizationForm.FormD);
            var chars = input.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        // ======================================
        // 📋 Các API LẤY DỮ LIỆU
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
