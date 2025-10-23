using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using PTJ_Service.LocationService;
using System.Security.Cryptography;
using System.Text;
using JobSeekerPostModel = PTJ_Models.Models.JobSeekerPost;

namespace PTJ_Service.JobSeekerPostService
{
    public class JobSeekerPostService : IJobSeekerPostService
    {
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;
        private readonly OpenMapService _map;

        public JobSeekerPostService(JobMatchingDbContext db, IAIService ai, OpenMapService map)
        {
            _db = db;
            _ai = ai;
            _map = map;
        }

        // =========================================================
        // 🧠 TẠO BÀI ĐĂNG + GỢI Ý VIỆC LÀM (AI + ĐỊA LÝ)
        // =========================================================
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto)
        {
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

            // 🧩 Embedding chỉ gồm nội dung (không chứa location)
            var (vector, hash) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}."
            );

            // Upsert vector vào AI store
            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title ?? "",
                    category = dto.CategoryID ?? 0,
                    postId = post.JobSeekerPostId
                });

            // 🔎 Tìm EmployerPost tương tự về nội dung
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);
            if (!matches.Any())
            {
                return new JobSeekerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedJobs = new List<AIResultDto>()
                };
            }

            var scored = await ScoreAndFilterJobsAsync(
                matches,
                dto.CategoryID,
                dto.PreferredLocation ?? "",
                dto.Title ?? ""
            );

            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, 5);

            // 🔖 Việc đã lưu
            var savedIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == post.UserId)
                .Select(x => x.EmployerPostId)
                .ToListAsync();

            var suggestions = scored
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"EmployerPost:{x.Job.EmployerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Job.EmployerPostId,
                        x.Job.Title,
                        x.Job.Location,
                        x.Job.WorkHours,
                        EmployerName = x.Job.User.Username,
                        IsSaved = savedIds.Contains(x.Job.EmployerPostId)
                    }
                })
                .ToList();

            return new JobSeekerPostResultDto
            {
                Post = await BuildCleanPostDto(post),
                SuggestedJobs = suggestions
            };
        }

        // =========================================================
        // 🔁 LÀM MỚI GỢI Ý
        // =========================================================
        public async Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId)
        {
            var post = await _db.JobSeekerPosts.FindAsync(jobSeekerPostId);
            if (post == null) throw new Exception("Không tìm thấy bài đăng.");

            var (vector, _) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}."
            );

            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);
            if (!matches.Any())
            {
                return new JobSeekerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedJobs = new List<AIResultDto>()
                };
            }

            var scored = await ScoreAndFilterJobsAsync(
                matches,
                post.CategoryId,
                post.PreferredLocation ?? "",
                post.Title ?? ""
            );

            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, 5);

            var savedIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == post.UserId)
                .Select(x => x.EmployerPostId)
                .ToListAsync();

            var suggestions = scored
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"EmployerPost:{x.Job.EmployerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Job.EmployerPostId,
                        x.Job.Title,
                        x.Job.Location,
                        x.Job.WorkHours,
                        EmployerName = x.Job.User.Username,
                        IsSaved = savedIds.Contains(x.Job.EmployerPostId)
                    }
                })
                .ToList();

            return new JobSeekerPostResultDto
            {
                Post = await BuildCleanPostDto(post),
                SuggestedJobs = suggestions
            };
        }

        // =========================================================
        // ⭐ SHORTLIST
        // =========================================================
        public async Task SaveJobAsync(SaveJobDto dto)
        {
            bool exists = await _db.JobSeekerShortlistedJobs
                .AnyAsync(x => x.JobSeekerId == dto.JobSeekerId && x.EmployerPostId == dto.EmployerPostId);

            if (!exists)
            {
                _db.JobSeekerShortlistedJobs.Add(new JobSeekerShortlistedJob
                {
                    JobSeekerId = dto.JobSeekerId,
                    EmployerPostId = dto.EmployerPostId,
                    Note = dto.Note,
                    AddedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnsaveJobAsync(SaveJobDto dto)
        {
            var record = await _db.JobSeekerShortlistedJobs
                .FirstOrDefaultAsync(x => x.JobSeekerId == dto.JobSeekerId && x.EmployerPostId == dto.EmployerPostId);

            if (record != null)
            {
                _db.JobSeekerShortlistedJobs.Remove(record);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<object>> GetSavedJobsAsync(int jobSeekerId)
        {
            return await _db.JobSeekerShortlistedJobs
                .Include(x => x.EmployerPost)
                .ThenInclude(e => e.User)
                .Where(x => x.JobSeekerId == jobSeekerId)
                .Select(x => new
                {
                    x.EmployerPostId,
                    x.EmployerPost.Title,
                    x.EmployerPost.Location,
                    EmployerName = x.EmployerPost.User.Username,
                    x.Note,
                    x.AddedAt
                })
                .ToListAsync();
        }

        // =========================================================
        // 📋 CRUD
        // =========================================================
        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync()
        {
            return await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = x.JobSeekerPostId,
                    Title = x.Title,
                    Description = x.Description,
                    PreferredLocation = x.PreferredLocation,
                    CategoryName = x.Category != null ? x.Category.Name : null,
                    SeekerName = x.User.Username,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
        {
            return await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = x.JobSeekerPostId,
                    Title = x.Title,
                    Description = x.Description,
                    PreferredLocation = x.PreferredLocation,
                    CategoryName = x.Category != null ? x.Category.Name : null,
                    SeekerName = x.User.Username,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<JobSeekerPostDtoOut?> GetByIdAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .Where(x => x.JobSeekerPostId == id)
                .Select(x => new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = x.JobSeekerPostId,
                    Title = x.Title,
                    Description = x.Description,
                    PreferredLocation = x.PreferredLocation,
                    CategoryName = x.Category != null ? x.Category.Name : null,
                    SeekerName = x.User.Username,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var post = await _db.JobSeekerPosts.FindAsync(id);
            if (post == null) return false;

            post.Status = "Deleted";
            post.UpdatedAt = DateTime.Now;

            var targets = _db.AiMatchSuggestions
                .Where(s => s.TargetType == "JobSeekerPost" && s.TargetId == id);
            _db.AiMatchSuggestions.RemoveRange(targets);

            await _db.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // ⚙️ SCORING = EMBEDDING + LOCATION + TITLE
        // =========================================================
        private async Task<List<(EmployerPost Job, double Score)>> ScoreAndFilterJobsAsync(
            List<(string Id, double Score)> matches,
            int? categoryId,
            string seekerLocation,
            string seekerTitle)
        {
            var list = new List<(EmployerPost, double)>();

            foreach (var m in matches)
            {
                if (!m.Id.StartsWith("EmployerPost:")) continue;
                if (!int.TryParse(m.Id.Split(':')[1], out var jobId)) continue;

                var job = await _db.EmployerPosts
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.EmployerPostId == jobId && x.Status == "Active");

                if (job == null) continue;
                if (categoryId.HasValue && job.CategoryId != categoryId) continue;

                double score = await ComputeHybridScoreAsync(
                    m.Score,
                    seekerLocation,
                    seekerTitle,
                    job.Location,
                    job.Title);

                list.Add((job, score));
            }

            return list;
        }

        private async Task<double> ComputeHybridScoreAsync(
            double embeddingScore,
            string seekerLocation,
            string seekerTitle,
            string? employerLocation,
            string? employerTitle)
        {
            const double W_EMBED = 0.7;
            const double W_LOC = 0.2;
            const double W_TITLE = 0.1;

            double locScore = 0.5;
            double titleScore = 0.0;

            if (!string.IsNullOrWhiteSpace(seekerLocation) && !string.IsNullOrWhiteSpace(employerLocation))
            {
                try
                {
                    var a = await _map.GetCoordinatesAsync(seekerLocation);
                    var b = await _map.GetCoordinatesAsync(employerLocation);
                    if (a != null && b != null)
                    {
                        var d = _map.ComputeDistanceKm(a.Value.lat, a.Value.lng, b.Value.lat, b.Value.lng);
                        if (d <= 2) locScore = 1;
                        else if (d <= 5) locScore = 0.9;
                        else if (d <= 10) locScore = 0.7;
                        else if (d <= 30) locScore = 0.5;
                        else if (d <= 50) locScore = 0.3;
                        else locScore = 0;
                    }
                }
                catch { locScore = 0.5; }
            }

            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var s = seekerTitle.ToLower();
                var e = employerTitle.ToLower();
                if (s.Contains(e) || e.Contains(s)) titleScore = 1;
                else if (e.Split(' ').Any(w => s.Contains(w))) titleScore = 0.6;
            }

            var hybrid = (embeddingScore * W_EMBED) + (locScore * W_LOC) + (titleScore * W_TITLE);
            return Math.Clamp(hybrid, 0, 1);
        }

        // =========================================================
        // 🧩 EMBEDDING CACHE
        // =========================================================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
        {
            // 🔹 Tạo hash từ nội dung
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            // 🔹 Kiểm tra đã có embedding chưa
            var embed = await _db.AiEmbeddingStatuses
                .FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId);

            // 🔹 Nếu trùng hash và đã có vectorData thì dùng lại cache
            if (embed != null && embed.ContentHash == hash && !string.IsNullOrEmpty(embed.VectorData))
            {
                var cached = JsonConvert.DeserializeObject<float[]>(embed.VectorData!)!;
                return (cached, hash);
            }

            // 🔹 Gọi AI tạo embedding mới
            var vector = await _ai.CreateEmbeddingAsync(text);
            var jsonVec = JsonConvert.SerializeObject(vector);

            if (embed == null)
            {
                // 🆕 THÊM MỚI
                embed = new AiEmbeddingStatus
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ContentHash = hash,
                    Model = "text-embedding-3-large",
                    VectorDim = vector.Length,
                    PineconeId = $"{entityType}:{entityId}",   // ⚠️ DÒNG QUAN TRỌNG — THÊM NÀY
                    Status = "OK",
                    UpdatedAt = DateTime.Now,
                    VectorData = jsonVec
                };
                _db.AiEmbeddingStatuses.Add(embed);
            }
            else
            {
                // 🧩 CẬP NHẬT LẠI (đã có record cũ)
                embed.ContentHash = hash;
                embed.VectorData = jsonVec;
                embed.UpdatedAt = DateTime.Now;

                // nếu cũ chưa có PineconeId thì bổ sung luôn
                if (string.IsNullOrEmpty(embed.PineconeId))
                    embed.PineconeId = $"{entityType}:{entityId}";
            }

            await _db.SaveChangesAsync();
            return (vector, hash);
        }


        private async Task UpsertSuggestionsAsync(
            string sourceType, int sourceId, string targetType,
            List<(EmployerPost Job, double Score)> scored, int keepTop)
        {
            var top = scored.OrderByDescending(x => x.Score).Take(keepTop).ToList();
            var keepIds = top.Select(x => x.Job.EmployerPostId).ToHashSet();

            foreach (var (job, score) in top)
            {
                var exist = await _db.AiMatchSuggestions.FirstOrDefaultAsync(x =>
                    x.SourceType == sourceType && x.SourceId == sourceId &&
                    x.TargetType == targetType && x.TargetId == job.EmployerPostId);

                if (exist == null)
                {
                    _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                    {
                        SourceType = sourceType,
                        SourceId = sourceId,
                        TargetType = targetType,
                        TargetId = job.EmployerPostId,
                        RawScore = score,
                        MatchPercent = (int)(score * 100),
                        Reason = "AI đề xuất việc làm",
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    exist.RawScore = score;
                    exist.MatchPercent = (int)(score * 100);
                    exist.Reason = "AI cập nhật đề xuất";
                    exist.UpdatedAt = DateTime.Now;
                }
            }

            var obsolete = await _db.AiMatchSuggestions
                .Where(x => x.SourceType == sourceType &&
                            x.SourceId == sourceId &&
                            x.TargetType == targetType &&
                            !keepIds.Contains(x.TargetId))
                .ToListAsync();

            if (obsolete.Any()) _db.AiMatchSuggestions.RemoveRange(obsolete);
            await _db.SaveChangesAsync();
        }

        private async Task<JobSeekerPostDtoOut> BuildCleanPostDto(JobSeekerPostModel post)
        {
            var category = await _db.Categories.FindAsync(post.CategoryId);
            var user = await _db.Users.FindAsync(post.UserId);

            return new JobSeekerPostDtoOut
            {
                JobSeekerPostId = post.JobSeekerPostId,
                Title = post.Title,
                Description = post.Description,
                PreferredLocation = post.PreferredLocation,
                CategoryName = category?.Name,
                SeekerName = user?.Username ?? "",
                CreatedAt = post.CreatedAt,
                Status = post.Status
            };
        }
    }
}
