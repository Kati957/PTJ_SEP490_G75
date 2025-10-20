using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
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
            // 1️⃣ Lưu bài đăng ứng viên
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

            // 2️⃣ Chuẩn hoá text cho embedding
            string text = $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}. Khu vực: {dto.PreferredLocation}.";
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3️⃣ Tạo embedding
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 4️⃣ Ghi trạng thái embedding
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

            // 5️⃣ Upsert lên Pinecone
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

            // 6️⃣ Query tương tự trong employer_posts
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);
            var allJobs = new List<(dynamic Job, double Score)>();

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
                        x.CategoryId,
                        EmployerName = x.User.Username
                    })
                    .FirstOrDefaultAsync();

                if (job == null) continue;

                // ❌ Bỏ qua nếu khác Category
                if (dto.CategoryID != job.CategoryId) continue;

                double hybridScore = ComputeHybridScore(
                    embeddingScore: m.Score,
                    seekerLocation: dto.PreferredLocation ?? "",
                    seekerCategoryId: dto.CategoryID,
                    seekerTitle: dto.Title ?? "",
                    jobLocation: job.Location,
                    jobCategoryId: job.CategoryId,
                    jobTitle: job.Title
                );

                allJobs.Add((job, hybridScore));
            }

            // 7️⃣ Sắp xếp theo điểm (có location khác vẫn được gợi ý nhưng điểm thấp)
            var sortedJobs = allJobs
                .OrderByDescending(j => j.Score)
                .Take(5)
                .ToList();

            // 8️⃣ Ghi DB & trả kết quả
            var suggestions = new List<AIResultDto>();
            foreach (var (job, hybridScore) in sortedJobs)
            {
                _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                {
                    SourceType = "JobSeekerPost",
                    SourceId = post.JobSeekerPostId,
                    TargetType = "EmployerPost",
                    TargetId = job.EmployerPostId,
                    RawScore = hybridScore,
                    MatchPercent = (int)Math.Round(hybridScore * 100),
                    Reason = $"AI gợi ý (ưu tiên khu vực gần, cùng Category)",
                    CreatedAt = DateTime.Now
                });

                suggestions.Add(new AIResultDto
                {
                    Id = $"EmployerPost:{job.EmployerPostId}",
                    Score = Math.Round(hybridScore * 100, 2),
                    ExtraInfo = job
                });
            }

            await _db.SaveChangesAsync();

            return new JobSeekerPostResultDto
            {
                Post = post,
                SuggestedJobs = suggestions
            };
        }

        // 🧮 Tính điểm hybrid tổng hợp
        private double ComputeHybridScore(
            double embeddingScore,
            string seekerLocation,
            int? seekerCategoryId,
            string seekerTitle,
            string? jobLocation,
            int? jobCategoryId,
            string? jobTitle)
        {
            double locationBonus = 0;
            double categoryBonus = 0;
            double titleBonus = 0;
            double locationPenalty = 1.0;

            var sLoc = NormalizeString(seekerLocation);
            var jLoc = NormalizeString(jobLocation ?? "");

            // 1️⃣ Ưu tiên địa điểm (và phạt nếu khác)
            if (!string.IsNullOrEmpty(sLoc) && !string.IsNullOrEmpty(jLoc))
            {
                if (sLoc == jLoc)
                    locationBonus = 0.35;
                else if (sLoc.Contains(jLoc) || jLoc.Contains(sLoc))
                    locationBonus = 0.25;
                else if (sLoc.Split(' ').Any(w => jLoc.Contains(w)))
                    locationBonus = 0.15;
                else
                    locationPenalty = 0.8; // khác khu vực: giảm 20%
            }

            // 2️⃣ Ưu tiên loại công việc
            if (seekerCategoryId.HasValue && jobCategoryId.HasValue)
            {
                if (seekerCategoryId == jobCategoryId)
                    categoryBonus = 0.20;
            }

            // 3️⃣ Ưu tiên tiêu đề
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(jobTitle))
            {
                var sTitle = seekerTitle.ToLowerInvariant();
                var jTitle = jobTitle.ToLowerInvariant();

                if (sTitle.Contains(jTitle) || jTitle.Contains(sTitle))
                    titleBonus = 0.15;
            }

            double hybrid = (embeddingScore + locationBonus + categoryBonus + titleBonus) * locationPenalty;
            if (hybrid > 1) hybrid = 1;
            return hybrid;
        }

        // 🔣 Bỏ dấu tiếng Việt & chuẩn hóa chuỗi
        private string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.ToLowerInvariant();
            input = input.Normalize(NormalizationForm.FormD);
            var chars = input
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        // ==============================
        // 📋 Các hàm GET dữ liệu
        // ==============================

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

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
        {
            return await _db.JobSeekerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId)
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
