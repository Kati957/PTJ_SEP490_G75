using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using System.Globalization;
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

        // ===========================================
        // 🧠 Tạo bài đăng JobSeeker + AI Matching + Pending
        // ===========================================
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto)
        {
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

            string text = $"{dto.Title}. {dto.Description}. Giờ làm: {dto.PreferredWorkHours}. Khu vực: {dto.PreferredLocation}.";
            var (vector, hash) = await EnsureEmbeddingAsync("JobSeekerPost", post.JobSeekerPostId, text);

            await _ai.UpsertVectorAsync("job_seeker_posts", $"JobSeekerPost:{post.JobSeekerPostId}", vector, new
            {
                title = dto.Title ?? "",
                location = dto.PreferredLocation ?? "",
                category = dto.CategoryID ?? 0,
                postId = post.JobSeekerPostId
            });

            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);

            // Không có việc phù hợp -> lưu pending
            if (!matches.Any())
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

                return new JobSeekerPostResultDto { Post = post, SuggestedJobs = new() };
            }

            var suggestions = await ScoreAndFilterJobsAsync(matches, dto.CategoryID, dto.PreferredLocation ?? "", dto.Title ?? "");
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", suggestions, 5);

            // Xóa pending nếu có
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            var result = suggestions.OrderByDescending(x => x.Score).Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"EmployerPost:{x.Job.EmployerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Job.EmployerPostId,
                        x.Job.Title,
                        x.Job.Location,
                        EmployerName = x.Job.User.Username
                    }
                }).ToList();

            return new JobSeekerPostResultDto { Post = post, SuggestedJobs = result };
        }

        // ===========================================
        // 🔁 Làm mới đề xuất (có pending logic)
        // ===========================================
        public async Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId)
        {
            var post = await _db.JobSeekerPosts.FindAsync(jobSeekerPostId);
            if (post == null) throw new Exception("Không tìm thấy bài đăng.");

            string text = $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}. Khu vực: {post.PreferredLocation}.";
            var (vector, hash) = await EnsureEmbeddingAsync("JobSeekerPost", post.JobSeekerPostId, text);

            await _ai.UpsertVectorAsync("job_seeker_posts", $"JobSeekerPost:{post.JobSeekerPostId}", vector, new
            {
                title = post.Title,
                location = post.PreferredLocation,
                category = post.CategoryId,
                postId = post.JobSeekerPostId
            });

            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 20);

            if (!matches.Any())
            {
                if (!await _db.AiContentForEmbeddings
                    .AnyAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId))
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

                return new JobSeekerPostResultDto { Post = post, SuggestedJobs = new() };
            }

            // Có việc -> xóa pending
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "JobSeekerPost" && x.EntityId == post.JobSeekerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            var suggestions = await ScoreAndFilterJobsAsync(matches, post.CategoryId, post.PreferredLocation ?? "", post.Title ?? "");
            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", suggestions, 5);

            var result = suggestions.OrderByDescending(x => x.Score).Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"EmployerPost:{x.Job.EmployerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Job.EmployerPostId,
                        x.Job.Title,
                        x.Job.Location,
                        EmployerName = x.Job.User.Username
                    }
                }).ToList();

            return new JobSeekerPostResultDto { Post = post, SuggestedJobs = result };
        }

        // ===========================================
        // 💾 Shortlist công việc yêu thích
        // ===========================================
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
                .ThenInclude(p => p.User)
                .Where(x => x.JobSeekerId == jobSeekerId)
                .Select(x => new
                {
                    x.EmployerPostId,
                    JobTitle = x.EmployerPost.Title,
                    EmployerName = x.EmployerPost.User.Username,
                    x.AddedAt,
                    x.Note
                })
                .ToListAsync();
        }

        // ===========================================
        // 📋 CRUD cơ bản
        // ===========================================
        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync()
        {
            return await _db.JobSeekerPosts
                .Include(u => u.User)
                .Include(c => c.Category)
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
                }).ToListAsync();
        }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
        {
            return await _db.JobSeekerPosts
                .Include(u => u.User)
                .Include(c => c.Category)
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
                }).ToListAsync();
        }
        public async Task<JobSeekerPostDtoOut?> GetByIdAsync(int id)
        {
            return await _db.JobSeekerPosts
                .Include(u => u.User)
                .Include(c => c.Category)
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

        // ===========================================
        // ⚙️ Helper
        // ===========================================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
        {
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            var existing = await _db.AiEmbeddingStatuses
                .FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId);

            if (existing != null && existing.ContentHash == hash && !string.IsNullOrEmpty(existing.VectorData))
            {
                var cached = JsonConvert.DeserializeObject<float[]>(existing.VectorData!);
                return (cached!, hash);
            }

            var vector = await _ai.CreateEmbeddingAsync(text);
            var jsonVec = JsonConvert.SerializeObject(vector);

            if (existing == null)
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
                    VectorData = jsonVec,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                existing.ContentHash = hash;
                existing.VectorData = jsonVec;
                existing.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return (vector, hash);
        }

        private string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            return new string(input.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
                .Normalize(NormalizationForm.FormC);
        }

        private async Task<List<(EmployerPost Job, double Score)>> ScoreAndFilterJobsAsync(
            List<(string Id, double Score)> matches,
            int? categoryId, string seekerLoc, string seekerTitle)
        {
            var list = new List<(EmployerPost, double)>();
            foreach (var m in matches)
            {
                if (!m.Id.StartsWith("EmployerPost:")) continue;
                int jobId = int.Parse(m.Id.Split(':')[1]);

                var job = await _db.EmployerPosts.Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.EmployerPostId == jobId);
                if (job == null) continue;

                if (categoryId.HasValue && job.CategoryId.HasValue && job.CategoryId != categoryId) continue;

                double score = ComputeHybridScore(m.Score, seekerLoc, seekerTitle, job.Location, job.Title);
                list.Add((job, score));
            }
            return list;
        }

        private double ComputeHybridScore(double emb, string sLoc, string sTitle, string? jLoc, string? jTitle)
        {
            double bonusLoc = 0, bonusTitle = 0, penalty = 1;
            sLoc = Normalize(sLoc);
            jLoc = Normalize(jLoc ?? "");

            if (sLoc == jLoc) bonusLoc = 0.3;
            else if (sLoc.Contains(jLoc) || jLoc.Contains(sLoc)) bonusLoc = 0.2;
            else penalty = 0.85;

            if (!string.IsNullOrEmpty(sTitle) && !string.IsNullOrEmpty(jTitle))
            {
                var s = sTitle.ToLowerInvariant();
                var j = jTitle.ToLowerInvariant();
                if (s.Contains(j) || j.Contains(s)) bonusTitle = 0.15;
            }

            var hybrid = (emb + bonusLoc + bonusTitle) * penalty;
            return hybrid > 1 ? 1 : hybrid;
        }

        private async Task UpsertSuggestionsAsync(
            string sourceType, int sourceId, string targetType,
            List<(EmployerPost Job, double Score)> scored, int keepTop)
        {
            var top = scored.OrderByDescending(x => x.Score).Take(keepTop).ToList();
            var targetIds = top.Select(t => t.Job.EmployerPostId).ToHashSet();

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
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    exist.RawScore = score;
                    exist.MatchPercent = (int)Math.Round(score * 100);
                    exist.UpdatedAt = DateTime.Now;
                }
            }

            var obsolete = await _db.AiMatchSuggestions
                .Where(x => x.SourceType == sourceType && x.SourceId == sourceId && !targetIds.Contains(x.TargetId))
                .ToListAsync();

            if (obsolete.Any()) _db.AiMatchSuggestions.RemoveRange(obsolete);

            await _db.SaveChangesAsync();
        }
    }

}
