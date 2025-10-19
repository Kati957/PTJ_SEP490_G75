using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

using PTJ_Models.Models;
using PTJ_Models.DTO;

// ⚠️ Alias để tránh trùng tên namespace với class
using EmployerPostModel = PTJ_Models.Models.EmployerPost;
using PTJ_Service.EmployerPostService;

namespace PTJ_Service.EmployerPostService
{
    public class EmployerPostService : IEmployerPostService
    {
        private readonly JobMatchingDbContext _db;
        private readonly HttpClient _http;
        private readonly string _openAiKey;

        public EmployerPostService(JobMatchingDbContext db, IConfiguration config)
        {
            _db = db;
            _http = new HttpClient();
            _openAiKey = config["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI key in appsettings.json");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        }

        // =====================================
        // 1️⃣ TẠO BÀI ĐĂNG + GỌI EMBEDDING
        // =====================================
        public async Task<EmployerPostModel> CreateEmployerPostAsync(EmployerPostDto dto)
        {
            // 1️⃣ Lưu bài đăng gốc
            var post = new EmployerPostModel
            {
                
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

            // 2️⃣ Chuẩn bị nội dung cho embedding
            string canonicalText =
                $"{dto.Title}\n{dto.Description}\nYêu cầu: {dto.Requirements}\nĐịa điểm: {dto.Location}\nLương: {dto.Salary}";
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText)));

            var aiContent = new AiContentForEmbedding
            {
                EntityType = "EmployerPost",
                EntityId = post.EmployerPostId,
                Lang = "vi",
                CanonicalText = canonicalText,
                Hash = hash,
                LastPreparedAt = DateTime.Now
            };

            _db.AiContentForEmbeddings.Add(aiContent);
            await _db.SaveChangesAsync();

            // 3️⃣ Gọi OpenAI API để tạo embedding
            var embedding = await GenerateEmbeddingAsync(canonicalText);

            // 4️⃣ Lưu trạng thái embedding
            var pineconeId = $"EmployerPost-{post.EmployerPostId}";
            var status = new AiEmbeddingStatus
            {
                EntityType = "EmployerPost",
                EntityId = post.EmployerPostId,
                Model = "text-embedding-3-large",
                VectorDim = embedding.Count,
                PineconeId = pineconeId,
                ContentHash = hash,
                Status = "OK",
                UpdatedAt = DateTime.Now
            };

            _db.AiEmbeddingStatuses.Add(status);
            await _db.SaveChangesAsync();

            return post;
        }

        // =====================================
        // 2️⃣ LẤY DANH SÁCH BÀI ĐĂNG
        // =====================================
        public async Task<IEnumerable<EmployerPostModel>> GetAllAsync()
        {
            return await _db.EmployerPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // =====================================
        // 3️⃣ LẤY BÀI ĐĂNG THEO ID
        // =====================================
        public async Task<EmployerPostModel?> GetByIdAsync(int id)
        {
            return await _db.EmployerPosts
                .FirstOrDefaultAsync(p => p.EmployerPostId == id);
        }

        // =====================================
        // 4️⃣ XOÁ BÀI ĐĂNG
        // =====================================
        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _db.EmployerPosts.FindAsync(id);
            if (post == null) return false;

            _db.EmployerPosts.Remove(post);
            await _db.SaveChangesAsync();
            return true;
        }

        // =====================================
        // 🔹 HÀM GỌI API OPENAI
        // =====================================
        private async Task<List<float>> GenerateEmbeddingAsync(string text)
        {
            var request = new
            {
                model = "text-embedding-3-large",
                input = text
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("https://api.openai.com/v1/embeddings", content);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(json)!;

            var list = ((IEnumerable<dynamic>)result.data[0].embedding)
                .Select(x => (float)x)
                .ToList();

            return list;
        }
    }
}
