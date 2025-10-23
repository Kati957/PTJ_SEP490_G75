using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Models.DTO;
using PTJ_Models.Models;
using PTJ_Service.AIService;
using PTJ_Service.LocationService;
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
        private readonly OpenMapService _map;

        public EmployerPostService(JobMatchingDbContext db, IAIService ai, OpenMapService map)
        {
            _db = db;
            _ai = ai;
            _map = map;
        }

        // =========================================================
        // CREATE + AI SUGGESTIONS (có pending khi chưa có kết quả)
        // =========================================================
        public async Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostDto dto)
        {
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
                UpdatedAt = DateTime.Now,
                Status = "Active"
            };

            _db.EmployerPosts.Add(post);
            await _db.SaveChangesAsync();

            // 🧠 Chuẩn bị nội dung embedding
            var (vector, hash) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
               $"{dto.Title}. {dto.Description}. Yêu cầu: {dto.Requirements}. Lương: {dto.Salary}"
            );

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

            // 🔍 Query các ứng viên tương tự
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 20);

            if (!matches.Any())
            {
                // Không có ứng viên -> thêm pending
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                {
                    EntityType = "EmployerPost",
                    EntityId = post.EmployerPostId,
                    Lang = "vi",
                    CanonicalText = $"{dto.Title}. {dto.Description}. {dto.Requirements}. {dto.Location}. {dto.Salary}",
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return new EmployerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                };
            }

            // ✅ Có kết quả -> chấm điểm hybrid
            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                mustMatchCategoryId: dto.CategoryID,
                employerLocation: dto.Location ?? "",
                employerTitle: dto.Title ?? ""
            );

            await UpsertSuggestionsAsync("EmployerPost", post.EmployerPostId, "JobSeekerPost", scored, keepTop: 5);

            // Xóa pending cũ
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "EmployerPost" && x.EntityId == post.EmployerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            // 🔖 Lấy danh sách đã lưu
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == post.EmployerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            var suggestions = scored
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"JobSeekerPost:{x.Seeker.JobSeekerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Seeker.JobSeekerPostId,
                        x.Seeker.Title,
                        x.Seeker.PreferredLocation,
                        x.Seeker.PreferredWorkHours,
                        SeekerName = x.Seeker.User.Username,
                        IsSaved = savedIds.Contains(x.Seeker.JobSeekerPostId)
                    }
                })
                .ToList();

            return new EmployerPostResultDto
            {
                Post = await BuildCleanPostDto(post),
                SuggestedCandidates = suggestions
            };
        }

        // =========================================================
        // REFRESH GỢI Ý
        // =========================================================
        public async Task<EmployerPostResultDto> RefreshSuggestionsAsync(int employerPostId)
        {
            var post = await _db.EmployerPosts.FindAsync(employerPostId);
            if (post == null) throw new Exception("Bài đăng không tồn tại.");

            var (vector, hash) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
                $"{post.Title}. {post.Description}. Yêu cầu: {post.Requirements}. Địa điểm: {post.Location}. Lương: {post.Salary}"
            );

            await _ai.UpsertVectorAsync(
                ns: "employer_posts",
                id: $"EmployerPost:{post.EmployerPostId}",
                vector: vector,
                metadata: new
                {
                    title = post.Title ?? "",
                    location = post.Location ?? "",
                    salary = post.Salary ?? 0,
                    postId = post.EmployerPostId
                });

            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 20);
            if (!matches.Any())
            {
                bool hasPending = await _db.AiContentForEmbeddings
                    .AnyAsync(x => x.EntityType == "EmployerPost" && x.EntityId == post.EmployerPostId);

                if (!hasPending)
                {
                    _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                    {
                        EntityType = "EmployerPost",
                        EntityId = post.EmployerPostId,
                        Lang = "vi",
                        CanonicalText = $"{post.Title}. {post.Description}. {post.Requirements}. {post.Location}. {post.Salary}",
                        Hash = hash,
                        LastPreparedAt = DateTime.Now
                    });
                    await _db.SaveChangesAsync();
                }

                return new EmployerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                };
            }

            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                mustMatchCategoryId: post.CategoryId,
                employerLocation: post.Location ?? "",
                employerTitle: post.Title ?? ""
            );

            await UpsertSuggestionsAsync("EmployerPost", post.EmployerPostId, "JobSeekerPost", scored, keepTop: 5);

            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            var suggestions = scored
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => new AIResultDto
                {
                    Id = $"JobSeekerPost:{x.Seeker.JobSeekerPostId}",
                    Score = Math.Round(x.Score * 100, 2),
                    ExtraInfo = new
                    {
                        x.Seeker.JobSeekerPostId,
                        x.Seeker.Title,
                        x.Seeker.PreferredLocation,
                        x.Seeker.PreferredWorkHours,
                        SeekerName = x.Seeker.User.Username,
                        IsSaved = savedIds.Contains(x.Seeker.JobSeekerPostId)
                    }
                })
                .ToList();

            return new EmployerPostResultDto
            {
                Post = await BuildCleanPostDto(post),
                SuggestedCandidates = suggestions
            };
        }

        // =========================================================
        // SCORING (category → location → title)
        // =========================================================
        private async Task<List<(JobSeekerPost Seeker, double Score)>> ScoreAndFilterCandidatesAsync(
            List<(string Id, double Score)> matches,
            int? mustMatchCategoryId,
            string employerLocation,
            string employerTitle)
        {
            var result = new List<(JobSeekerPost, double)>();

            foreach (var m in matches)
            {
                if (!m.Id.StartsWith("JobSeekerPost:")) continue;
                if (!int.TryParse(m.Id.Split(':')[1], out var seekerPostId)) continue;

                var seeker = await _db.JobSeekerPosts
                    .Include(x => x.User)
                    .Where(x => x.Status == "Active")
                    .FirstOrDefaultAsync(x => x.JobSeekerPostId == seekerPostId);

                if (seeker == null) continue;

                // Bắt buộc cùng Category
                if (mustMatchCategoryId.HasValue && seeker.CategoryId != mustMatchCategoryId) continue;

                // 🧮 Tính điểm hybrid
                double score = await ComputeHybridScoreAsync(
                    m.Score, employerLocation, employerTitle, seeker.PreferredLocation, seeker.Title);

                result.Add((seeker, score));
            }

            return result;
        }

        private async Task<double> ComputeHybridScoreAsync(
    double embeddingScore,
    string employerLocation,
    string employerTitle,
    string? seekerLocation,
    string? seekerTitle)
        {
            const double W_EMBED = 0.7;
            const double W_LOC = 0.2;
            const double W_TITLE = 0.1;

            double locationScore = 0.5;
            double titleScore = 0.0;

            // ✅ Tính khoảng cách thật giữa 2 vị trí
            if (!string.IsNullOrWhiteSpace(employerLocation) && !string.IsNullOrWhiteSpace(seekerLocation))
            {
                try
                {
                    var coord1 = await _map.GetCoordinatesAsync(employerLocation);
                    var coord2 = await _map.GetCoordinatesAsync(seekerLocation);

                    if (coord1 != null && coord2 != null)
                    {
                        double distKm = _map.ComputeDistanceKm(
                            coord1.Value.lat, coord1.Value.lng,
                            coord2.Value.lat, coord2.Value.lng);

                        // 📍 Xếp điểm theo khoảng cách
                        if (distKm <= 2) locationScore = 1.0;     // cùng phường / gần kề
                        else if (distKm <= 5) locationScore = 0.9;
                        else if (distKm <= 10) locationScore = 0.7;
                        else if (distKm <= 30) locationScore = 0.5;
                        else if (distKm <= 50) locationScore = 0.3;
                        else locationScore = 0.0; // quá xa
                    }
                }
                catch
                {
                    locationScore = 0.5;
                }
            }

            // 🧩 Title match
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var eTitle = employerTitle.ToLowerInvariant();
                var sTitle = seekerTitle.ToLowerInvariant();
                if (sTitle.Contains(eTitle) || eTitle.Contains(sTitle))
                    titleScore = 1.0;
                else if (eTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => sTitle.Contains(w)))
                    titleScore = 0.6;
            }

            double hybrid = (embeddingScore * W_EMBED) + (locationScore * W_LOC) + (titleScore * W_TITLE);
            return Math.Clamp(hybrid, 0, 1);
        }


        // =========================================================
        // HELPER: Upsert + Embedding Cache
        // =========================================================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
        {
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            var embed = await _db.AiEmbeddingStatuses.FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId);

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

        private async Task UpsertSuggestionsAsync(
            string sourceType, int sourceId, string targetType,
            List<(JobSeekerPost Seeker, double Score)> scored, int keepTop)
        {
            var top = scored.OrderByDescending(x => x.Score).Take(keepTop).ToList();
            var keepIds = top.Select(t => t.Seeker.JobSeekerPostId).ToHashSet();

            foreach (var (seeker, score) in top)
            {
                var exist = await _db.AiMatchSuggestions.FirstOrDefaultAsync(x =>
                    x.SourceType == sourceType &&
                    x.SourceId == sourceId &&
                    x.TargetType == targetType &&
                    x.TargetId == seeker.JobSeekerPostId);

                if (exist == null)
                {
                    _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                    {
                        SourceType = sourceType,
                        SourceId = sourceId,
                        TargetType = targetType,
                        TargetId = seeker.JobSeekerPostId,
                        RawScore = score,
                        MatchPercent = (int)Math.Round(score * 100),
                        Reason = "AI đề xuất ứng viên",
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

            var obsolete = await _db.AiMatchSuggestions
                .Where(x => x.SourceType == sourceType && x.SourceId == sourceId && x.TargetType == targetType && !keepIds.Contains(x.TargetId))
                .ToListAsync();

            if (obsolete.Any())
                _db.AiMatchSuggestions.RemoveRange(obsolete);

            await _db.SaveChangesAsync();
        }

        // =========================================================
        // SHORTLIST + CRUD
        // =========================================================
        public async Task SaveCandidateAsync(SaveCandidateDto dto)
        {
            bool exists = await _db.EmployerShortlistedCandidates
                .AnyAsync(x => x.EmployerPostId == dto.EmployerPostId && x.JobSeekerId == dto.JobSeekerId);

            if (!exists)
            {
                _db.EmployerShortlistedCandidates.Add(new EmployerShortlistedCandidate
                {
                    EmployerId = dto.EmployerId,
                    JobSeekerId = dto.JobSeekerId,
                    EmployerPostId = dto.EmployerPostId,
                    Note = dto.Note,
                    AddedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnsaveCandidateAsync(SaveCandidateDto dto)
        {
            var record = await _db.EmployerShortlistedCandidates
                .FirstOrDefaultAsync(x => x.EmployerPostId == dto.EmployerPostId && x.JobSeekerId == dto.JobSeekerId);
            if (record != null)
            {
                _db.EmployerShortlistedCandidates.Remove(record);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<object>> GetShortlistedByPostAsync(int employerPostId)
        {
            return await _db.EmployerShortlistedCandidates
                .Include(x => x.JobSeeker)
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => new
                {
                    x.JobSeekerId,
                    x.Note,
                    x.AddedAt,
                    JobSeekerName = x.JobSeeker.Username
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
        {
            return await _db.EmployerPosts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.Status == "Active") // ✅ Thêm dòng này
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
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId)
                .Where(p => p.Status == "Active") // ✅ Thêm dòng này
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

            post.Status = "Deleted";
            post.UpdatedAt = DateTime.Now;

            var targets = _db.AiMatchSuggestions
                .Where(s => s.TargetType == "EmployerPost" && s.TargetId == id);
            _db.AiMatchSuggestions.RemoveRange(targets);

            await _db.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // DTO Builder
        // =========================================================
        private async Task<EmployerPostDtoOut> BuildCleanPostDto(EmployerPostModel post)
        {
            var category = await _db.Categories.FindAsync(post.CategoryId);
            var user = await _db.Users.FindAsync(post.UserId);

            return new EmployerPostDtoOut
            {
                EmployerPostId = post.EmployerPostId,
                Title = post.Title,
                Description = post.Description,
                Salary = post.Salary,
                Requirements = post.Requirements,
                WorkHours = post.WorkHours,
                Location = post.Location,
                PhoneContact = post.PhoneContact,
                CategoryName = category?.Name,
                EmployerName = user?.Username ?? "",
                CreatedAt = post.CreatedAt,
                Status = post.Status
            };
        }
    }
}
