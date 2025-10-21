using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Globalization;
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

        // =========================================================
        // 🧠 TẠO BÀI ĐĂNG + GỢI Ý VIỆC LÀM (AI MATCHING)
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

            // 🔹 Tạo embedding vector (có cache theo hash)
            var (vector, hash) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}. Khu vực: {dto.PreferredLocation}."
            );

            // 🔹 Upsert lên Pinecone
            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                {
                    title = dto.Title ?? "",
                    location = dto.PreferredLocation ?? "",
                    category = dto.CategoryID ?? 0,
                    postId = post.JobSeekerPostId
                });

            // 🔹 Query sang employer_posts
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);

            if (!matches.Any())
            {
                // Ghi pending để scheduler re-try
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                {
                    EntityType = "JobSeekerPost",
                    EntityId = post.JobSeekerPostId,
                    Lang = "vi",
                    CanonicalText = $"{dto.Title}. {dto.Description}. {dto.PreferredWorkHours}. {dto.PreferredLocation}.",
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

            // 🔹 Lọc & tính điểm hybrid
            var scored = await ScoreAndFilterJobsAsync(matches, dto.CategoryID, dto.PreferredLocation ?? "", dto.Title ?? "");
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, keepTop: 5);

            // 🔹 Xóa pending nếu có
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            // 🔹 Đánh dấu job đã lưu
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
            if (post == null) throw new Exception("Không tìm thấy bài đăng ứng viên.");

            var (vector, hash) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}. Khu vực: {post.PreferredLocation}."
            );

            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{post.JobSeekerPostId}",
                vector: vector,
                metadata: new
                {
                    title = post.Title ?? "",
                    location = post.PreferredLocation ?? "",
                    category = post.CategoryId ?? 0,
                    postId = post.JobSeekerPostId
                });

            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);

            if (!matches.Any())
            {
                bool hasPending = await _db.AiContentForEmbeddings
                    .AnyAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId);

                if (!hasPending)
                {
                    _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                    {
                        EntityType = "JobSeekerPost",
                        EntityId = post.JobSeekerPostId,
                        Lang = "vi",
                        CanonicalText = $"{post.Title}. {post.Description}. {post.PreferredWorkHours}. {post.PreferredLocation}.",
                        Hash = hash,
                        LastPreparedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }

                return new JobSeekerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedJobs = new List<AIResultDto>()
                };
            }

            // Có kết quả → xoá pending
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            var scored = await ScoreAndFilterJobsAsync(matches, post.CategoryId, post.PreferredLocation ?? "", post.Title ?? "");
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, keepTop: 5);

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
        // 💾 LƯU / XOÁ CÔNG VIỆC YÊU THÍCH
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
        // CRUD CƠ BẢN
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
            await _db.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // 🧮 HELPER: EMBEDDING + SCORING + DTO BUILDER
        // =========================================================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
        {
            if (text.Length > 6000) text = text[..6000];
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
                .Where(x => x.Status == "Active")                  // 👈 thêm filter
                .FirstOrDefaultAsync(x => x.EmployerPostId == jobId);



                if (job == null) continue;

                // Lọc đúng Category trước khi chấm điểm
                if (categoryId.HasValue && job.CategoryId != categoryId) continue;

                double score = ComputeHybridScore(
                    m.Score,
                    seekerLocation,
                    seekerTitle,
                    job.Location,
                    job.Title);

                list.Add((job, score));
            }

            return list;
        }

        private async Task UpsertSuggestionsAsync(
            string sourceType, int sourceId, string targetType,
            List<(EmployerPost Job, double Score)> scored, int keepTop)
        {
            var top = scored.OrderByDescending(x => x.Score).Take(keepTop).ToList();
            var keepIds = top.Select(t => t.Job.EmployerPostId).ToHashSet();

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
                }
            }

            // Xóa các gợi ý cũ không còn trong top
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

        // =========================================================
        // ⚙️ ComputeHybridScore (đồng bộ với EmployerPostService)
        // =========================================================
        private double ComputeHybridScore(
            double embeddingScore,
            string seekerLocation,
            string seekerTitle,
            string? employerLocation,
            string? employerTitle)
        {
            double locationBonus = 0;
            double titleBonus = 0;
            double penalty = 1.0;

            var sLoc = Normalize(seekerLocation);
            var eLoc = Normalize(employerLocation ?? "");

            // Ưu tiên: cùng địa danh → cùng miền → phạt xa
            if (!string.IsNullOrEmpty(sLoc) && !string.IsNullOrEmpty(eLoc))
            {
                // 1) Cùng địa danh
                if (sLoc == eLoc || sLoc.Contains(eLoc) || eLoc.Contains(sLoc))
                {
                    locationBonus = 0.40; // rất gần
                }
                else
                {
                    // 2) Cùng miền Bắc/Trung/Nam
                    string[] north = { "ha noi", "hai phong", "bac ninh", "bac giang", "thai nguyen" };
                    string[] central = { "da nang", "hue", "quang nam", "quang ngai" };
                    string[] south = { "ho chi minh", "tp hcm", "tphcm", "binh duong", "dong nai", "can tho" };

                    bool sN = north.Any(l => sLoc.Contains(l));
                    bool sC = central.Any(l => sLoc.Contains(l));
                    bool sS = south.Any(l => sLoc.Contains(l));

                    bool eN = north.Any(l => eLoc.Contains(l));
                    bool eC = central.Any(l => eLoc.Contains(l));
                    bool eS = south.Any(l => eLoc.Contains(l));

                    if ((sN && eN) || (sC && eC) || (sS && eS))
                        locationBonus = 0.25; // cùng miền
                    else
                        penalty = 0.60; // khác miền → phạt mạnh
                }
            }

            // 🎯 Bonus tiêu đề gần nhau
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var sTitle = seekerTitle.ToLowerInvariant();
                var eTitle = employerTitle.ToLowerInvariant();
                if (sTitle.Contains(eTitle) || eTitle.Contains(sTitle))
                    titleBonus = 0.15;
            }

            var hybrid = (embeddingScore + locationBonus + titleBonus) * penalty;
            return hybrid > 1 ? 1 : hybrid;
        }

        private string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var chars = input.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        // ======================================
        // 🧩 Helper: Model -> DTO Out
        // ======================================
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
