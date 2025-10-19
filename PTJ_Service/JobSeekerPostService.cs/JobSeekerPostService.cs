using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Security.Cryptography;
using System.Text;

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

        // 🧠 Tạo bài đăng JobSeeker + gọi OpenAI + Pinecone + trả về gợi ý việc làm
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto)
        {
            // 1️⃣ Lưu bài đăng
            var post = new JobSeekerPost
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

            // 2️⃣ Chuẩn bị nội dung embedding
            string text = $"{dto.Title}. {dto.Description}. " +
                          $"Kinh nghiệm / Mong muốn: {dto.PreferredWorkHours}. " +
                          $"Khu vực: {dto.PreferredLocation}. Tuổi: {dto.Age}, Giới tính: {dto.Gender}";

            if (text.Length > 6000)
                text = text.Substring(0, 6000);

            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3️⃣ Gọi OpenAI tạo vector
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 4️⃣ Gửi vector lên Pinecone
            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title ?? "",
                    location = dto.PreferredLocation ?? "",
                    age = dto.Age ?? 0,
                    gender = dto.Gender ?? "",
                    postId = post.JobSeekerPostId
                }
            );

            // 5️⃣ So sánh với các bài tuyển dụng trong employer_posts
            var results = await _ai.QuerySimilarAsync("employer_posts", vector, 5);

            // 6️⃣ Nếu không có kết quả, lưu lại để AI xử lý sau
            if (!results.Any())
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

            // 7️⃣ Trả về kết quả (bao gồm bài post + danh sách gợi ý việc làm)
            return new JobSeekerPostResultDto
            {
                JobPost = post,
                SuggestedJobs = results.Select(r => new AIResultDto
                {
                    Id = r.Id,
                    Score = Math.Round(r.Score * 100, 2) // % độ tương đồng
                }).ToList()
            };
        }

        // =====================================
        // CRUD cơ bản
        // =====================================

        public async Task<IEnumerable<JobSeekerPost>> GetAllAsync()
        {
            return await _db.JobSeekerPosts
                .Include(p => p.Category)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<JobSeekerPost?> GetByIdAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(p => p.Category)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.JobSeekerPostId == id);
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
