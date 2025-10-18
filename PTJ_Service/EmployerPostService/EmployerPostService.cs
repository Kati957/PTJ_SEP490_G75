using Microsoft.EntityFrameworkCore;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Security.Cryptography;
using System.Text;

// alias tránh trùng namespace
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

        // 🧠 Tạo bài đăng + gọi OpenAI và Pinecone
        public async Task<EmployerPostModel> CreateEmployerPostAsync(EmployerPostDto dto)
        {
            // 1️⃣ Lưu bài đăng vào DB
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

            // 2️⃣ Chuẩn bị nội dung để embedding
            string text = $"{dto.Title}. {dto.Description}. " +
                          $"Yêu cầu: {dto.Requirements}. " +
                          $"Địa điểm: {dto.Location}. Lương: {dto.Salary}";
            if (text.Length > 6000)
                text = text.Substring(0, 6000);

            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 3️⃣ Gọi OpenAI tạo vector
            var vector = await _ai.CreateEmbeddingAsync(text);

            // 4️⃣ Gửi vector lên Pinecone
            await _ai.UpsertVectorAsync(
                ns: "employer_posts",
                id: $"EmployerPost:{post.EmployerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title,
                    location = dto.Location,
                    salary = dto.Salary,
                    postId = post.EmployerPostId
                }
            );

            // 5️⃣ So sánh với vector ứng viên (namespace khác)
            var results = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 5);

            // 6️⃣ Nếu không tìm thấy ứng viên phù hợp → lưu lại text để AI xử lý sau
            if (!results.Any())
            {
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

            return post;
        }

        // =====================================
        // Các CRUD cơ bản
        // =====================================

        public async Task<IEnumerable<EmployerPostModel>> GetAllAsync()
        {
            return await _db.EmployerPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<EmployerPostModel?> GetByIdAsync(int id)
        {
            return await _db.EmployerPosts.FirstOrDefaultAsync(p => p.EmployerPostId == id);
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
