using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models.DTO.PostDTO;
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
        private readonly IJobSeekerPostRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;
        private readonly OpenMapService _map;

        public JobSeekerPostService(
            IJobSeekerPostRepository repo,
            JobMatchingDbContext db,
            IAIService ai,
            OpenMapService map)
            {
            _repo = repo;
            _db = db;
            _ai = ai;
            _map = map;
            }

        // =========================================================
        // CREATE
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

            await _repo.AddAsync(post);

            // 🔥 FIX: cần SaveChanges để có ID thật (tránh JobSeekerPostId=0)
            await _db.SaveChangesAsync();

            // 🧠 Tạo embedding vector
            var (vector, hash) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}."
            );

            // 🧩 Upsert vector vào Pinecone / vector DB
            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                    {
                    title = dto.Title ?? "",
                    location = dto.PreferredLocation ?? "",
                    categoryId = dto.CategoryID ?? 0,
                    postId = post.JobSeekerPostId
                    });

            // 🔍 Truy vấn gợi ý việc làm tương tự
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 100);

            // ⛔ Nếu chưa có job nào để match (DB còn trống)
            if (!matches.Any())
                {
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                    {
                    EntityType = "JobSeekerPost",
                    EntityId = post.JobSeekerPostId,
                    Lang = "vi",
                    CanonicalText = $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}.",
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                    });
                await _db.SaveChangesAsync();

                return new JobSeekerPostResultDto
                    {
                    Post = await BuildCleanPostDto(post),
                    SuggestedJobs = new List<AIResultDto>()
                    };
                }

            // 🧮 Tính điểm hybrid và lọc theo category
            var scored = await ScoreAndFilterJobsAsync(
                matches,
                dto.CategoryID,
                dto.PreferredLocation ?? "",
                dto.Title ?? ""
            );

            // 💾 Lưu gợi ý top 5 vào bảng AiMatchSuggestions
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, keepTop: 5);

            // 🧾 Lấy danh sách job mà seeker đã lưu
            var savedIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == post.UserId)
                .Select(x => x.EmployerPostId)
                .ToListAsync();

            // 🧩 Chuẩn hóa danh sách gợi ý trả ra client
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
        // READ
        // =========================================================
        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync()
            {
            var posts = await _repo.GetAllAsync();
            return posts.Select(p => new JobSeekerPostDtoOut
                {
                JobSeekerPostId = p.JobSeekerPostId,
                Title = p.Title,
                Description = p.Description,
                PreferredLocation = p.PreferredLocation,
                CategoryName = p.Category?.Name,
                SeekerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status
                });
            }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
            {
            var posts = await _repo.GetByUserAsync(userId);
            return posts.Select(p => new JobSeekerPostDtoOut
                {
                JobSeekerPostId = p.JobSeekerPostId,
                Title = p.Title,
                Description = p.Description,
                PreferredLocation = p.PreferredLocation,
                CategoryName = p.Category?.Name,
                SeekerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status
                });
            }

        public async Task<JobSeekerPostDtoOut?> GetByIdAsync(int id)
            {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return null;

            return new JobSeekerPostDtoOut
                {
                JobSeekerPostId = post.JobSeekerPostId,
                Title = post.Title,
                Description = post.Description,
                PreferredLocation = post.PreferredLocation,
                CategoryName = post.Category?.Name,
                SeekerName = post.User.Username,
                CreatedAt = post.CreatedAt,
                Status = post.Status
                };
            }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task<JobSeekerPostDtoOut?> UpdateAsync(int id, JobSeekerPostDto dto)
            {
            var post = await _repo.GetByIdAsync(id);
            if (post == null || post.Status == "Deleted")
                return null;

            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Age = dto.Age;
            post.Gender = dto.Gender;
            post.PreferredWorkHours = dto.PreferredWorkHours;
            post.PreferredLocation = dto.PreferredLocation;
            post.CategoryId = dto.CategoryID;
            post.PhoneContact = dto.PhoneContact;
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);

            // Cập nhật embedding
            var (vector, _) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}."
            );

            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                    {
                    title = post.Title ?? "",
                    location = post.PreferredLocation ?? "",
                    categoryId = post.CategoryId ?? 0,
                    postId = post.JobSeekerPostId
                    });

            return await BuildCleanPostDto(post);
            }

        // =========================================================
        // DELETE (Soft)
        // =========================================================
        public async Task<bool> DeleteAsync(int id)
            {
            await _repo.SoftDeleteAsync(id);

            var targets = _db.AiMatchSuggestions
                .Where(s => (s.SourceType == "JobSeekerPost" && s.SourceId == id)
                         || (s.TargetType == "JobSeekerPost" && s.TargetId == id));

            _db.AiMatchSuggestions.RemoveRange(targets);
            await _db.SaveChangesAsync();

            return true;
            }

        // =========================================================
        // REFRESH SUGGESTIONS
        // =========================================================
        public async Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId)
            {
            var post = await _repo.GetByIdAsync(jobSeekerPostId);
            if (post == null)
                throw new Exception("Không tìm thấy bài đăng.");

            var (vector, _) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}."
            );

            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                    {
                    title = post.Title ?? "",
                    location = post.PreferredLocation ?? "",
                    categoryId = post.CategoryId ?? 0,
                    postId = post.JobSeekerPostId
                    });

            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 100);
            if (!matches.Any())
                {
                return new JobSeekerPostResultDto
                    {
                    Post = await BuildCleanPostDto(post),
                    SuggestedJobs = new List<AIResultDto>()
                    };
                }

            var scored = await ScoreAndFilterJobsAsync(matches, post.CategoryId, post.PreferredLocation ?? "", post.Title ?? "");
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, keepTop: 5);

            var savedIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == post.UserId)
                .Select(x => x.EmployerPostId)
                .ToListAsync();

            var suggestions = scored.OrderByDescending(x => x.Score)
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
                    }).ToList();

            return new JobSeekerPostResultDto
                {
                Post = await BuildCleanPostDto(post),
                SuggestedJobs = suggestions
                };
            }

        // =========================================================
        // SCORING (Hybrid)
        // =========================================================
        private async Task<List<(EmployerPost Job, double Score)>> ScoreAndFilterJobsAsync(
     List<(string Id, double Score)> matches,
     int? categoryId,
     string seekerLocation,
     string seekerTitle)
            {
            // ✅ Đặt tên cho tuple ở đây
            var list = new List<(EmployerPost Job, double Score)>();

            foreach (var m in matches)
                {
                if (!m.Id.StartsWith("EmployerPost:"))
                    continue;
                if (!int.TryParse(m.Id.Split(':')[1], out var jobId))
                    continue;

                var job = await _db.EmployerPosts
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.EmployerPostId == jobId && x.Status == "Active");

                if (job == null)
                    continue;
                if (categoryId.HasValue && job.CategoryId != categoryId)
                    continue;

                double score = await ComputeHybridScoreAsync(
                    m.Score, seekerLocation, seekerTitle, job.Location, job.Title);

                list.Add((job, score));
                }

            // 🧹 Lọc bỏ các job có score quá thấp (ví dụ < 0.4)
            return list.Where(x => x.Score >= 0.4).ToList();
            }


        private async Task<double> ComputeHybridScoreAsync(
            double embeddingScore,
            string locationA,
            string titleA,
            string? locationB,
            string? titleB)
            {
            const double W_EMBED = 0.7;
            const double W_LOC = 0.2;
            const double W_TITLE = 0.1;

            double locScore = 0.5;
            double titleScore = 0.5;

            // 🎯 Location
            if (!string.IsNullOrWhiteSpace(locationA) && !string.IsNullOrWhiteSpace(locationB))
                {
                try
                    {
                    var a = await _map.GetCoordinatesAsync(locationA);
                    var b = await _map.GetCoordinatesAsync(locationB);
                    if (a != null && b != null)
                        {
                        var d = _map.ComputeDistanceKm(a.Value.lat, a.Value.lng, b.Value.lat, b.Value.lng);
                        if (d <= 2)
                            locScore = 1;
                        else if (d <= 5)
                            locScore = 0.9;
                        else if (d <= 10)
                            locScore = 0.8;
                        else if (d <= 30)
                            locScore = 0.6;
                        else if (d <= 100)
                            locScore = 0.4;
                        else
                            locScore = 0.5; // nới nhẹ để không bị loại
                        }
                    }
                catch { locScore = 0.5; }
                }

            // 🎯 Title similarity
            if (!string.IsNullOrEmpty(titleA) && !string.IsNullOrEmpty(titleB))
                {
                try
                    {
                    var vecA = await GetOrCreateTitleEmbeddingAsync(titleA);
                    var vecB = await GetOrCreateTitleEmbeddingAsync(titleB);

                    double dot = 0, magA = 0, magB = 0;
                    for (int i = 0; i < vecA.Length; i++)
                        {
                        dot += vecA[i] * vecB[i];
                        magA += vecA[i] * vecA[i];
                        magB += vecB[i] * vecB[i];
                        }

                    double cosine = dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
                    titleScore = Math.Clamp(0.4 + (cosine * 0.6), 0, 1);
                    }
                catch { titleScore = 0.5; }
                }

            return Math.Clamp(
                (embeddingScore * W_EMBED) + (locScore * W_LOC) + (titleScore * W_TITLE),
                0, 1);
            }

        // =========================================================
        // SHORTLIST
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
                    }).ToListAsync();
            }

        // =========================================================
        // HELPERS
        // =========================================================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
            {
            if (text.Length > 6000)
                text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            var embed = await _db.AiEmbeddingStatuses
                .FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId);

            if (embed != null && embed.ContentHash == hash && !string.IsNullOrEmpty(embed.VectorData))
                {
                var cached = JsonConvert.DeserializeObject<float[]>(embed.VectorData!)!;
                return (cached, hash);
                }

            var vector = await _ai.CreateEmbeddingAsync(text);
            var jsonVec = JsonConvert.SerializeObject(vector);

            if (embed == null)
                {
                _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
                    {
                    EntityType = entityType,
                    EntityId = entityId,
                    ContentHash = hash,
                    Model = "text-embedding-3-large",
                    VectorDim = vector.Length,
                    PineconeId = $"{entityType}:{entityId}",
                    Status = "OK",
                    UpdatedAt = DateTime.Now,
                    VectorData = jsonVec
                    });
                }
            else
                {
                embed.ContentHash = hash;
                embed.VectorData = jsonVec;
                embed.UpdatedAt = DateTime.Now;
                }

            await _db.SaveChangesAsync();
            return (vector, hash);
            }

        private async Task<float[]> GetOrCreateTitleEmbeddingAsync(string title)
            {
            string entityType = "TitleCache";
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(title.ToLowerInvariant())));

            var cached = await _db.AiEmbeddingStatuses
                .FirstOrDefaultAsync(x => x.EntityType == entityType && x.ContentHash == hash);

            if (cached != null && !string.IsNullOrEmpty(cached.VectorData))
                return JsonConvert.DeserializeObject<float[]>(cached.VectorData!)!;

            var vector = await _ai.CreateEmbeddingAsync(title);
            var json = JsonConvert.SerializeObject(vector);

            _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
                {
                EntityType = entityType,
                EntityId = 0,
                ContentHash = hash,
                Model = "text-embedding-3-small",
                VectorDim = vector.Length,
                PineconeId = $"{entityType}:{hash[..12]}",
                Status = "OK",
                UpdatedAt = DateTime.Now,
                VectorData = json
                });
            await _db.SaveChangesAsync();

            return vector;
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

        private async Task UpsertSuggestionsAsync(
            string sourceType, int sourceId, string targetType,
            List<(EmployerPost Job, double Score)> scored, int keepTop)
            {
            var top = scored.OrderByDescending(x => x.Score).Take(keepTop).ToList();
            var keepIds = top.Select(x => x.Job.EmployerPostId).ToHashSet();

            foreach (var (job, score) in top)
                {
                var exist = await _db.AiMatchSuggestions.FirstOrDefaultAsync(x =>
                    x.SourceType == sourceType &&
                    x.SourceId == sourceId &&
                    x.TargetType == targetType &&
                    x.TargetId == job.EmployerPostId);

                if (exist == null)
                    {
                    _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                        {
                        SourceType = sourceType,
                        SourceId = sourceId,
                        TargetType = targetType,
                        TargetId = job.EmployerPostId,
                        RawScore = score,
                        MatchPercent = (int)Math.Round(score * 100),
                        Reason = "AI đề xuất việc làm",
                        CreatedAt = DateTime.Now
                        });
                    }
                else
                    {
                    exist.RawScore = score;
                    exist.MatchPercent = (int)Math.Round(score * 100);
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

            if (obsolete.Any())
                _db.AiMatchSuggestions.RemoveRange(obsolete);

            await _db.SaveChangesAsync();
            }
        }
    }
