using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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

            // Chuẩn bị nội dung & tạo embedding (có cache VectorData)
            var (vector, hash) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
                $"{dto.Title}. {dto.Description}. Yêu cầu: {dto.Requirements}. Địa điểm: {dto.Location}. Lương: {dto.Salary}"
            );

            // Lưu vector lên Pinecone
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

            // Query JobSeeker tương tự
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 20);

            if (!matches.Any())
            {
                // Chưa có ứng viên phù hợp -> ghi pending để scheduler xử lý lại sau
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
                    Post = await BuildCleanPostDto(post), // trả về entity; Controller map sang DTO Out để tránh vòng lặp
                    SuggestedCandidates = new List<AIResultDto>()
                };
            }

            // Có kết quả -> chấm điểm hybrid (ưu tiên theo quận/tỉnh/miền) + lưu gợi ý
            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                mustMatchCategoryId: dto.CategoryID,
                employerLocation: dto.Location ?? "",
                employerTitle: dto.Title ?? ""
            );

            await UpsertSuggestionsAsync("EmployerPost", post.EmployerPostId, "JobSeekerPost", scored, keepTop: 5);

            // Xoá pending nếu còn
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "EmployerPost" && x.EntityId == post.EmployerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
            }

            // Đánh dấu IsSaved cho top kết quả
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

        // ======================================
        // LÀM MỚI ĐỀ XUẤT (Refresh)
        // ======================================
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
                // Không có kết quả -> đảm bảo pending
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

            // Có kết quả -> xoá pending
            var pending = await _db.AiContentForEmbeddings
                .FirstOrDefaultAsync(x => x.EntityType == "EmployerPost" && x.EntityId == post.EmployerPostId);
            if (pending != null)
            {
                _db.AiContentForEmbeddings.Remove(pending);
                await _db.SaveChangesAsync();
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

        // =========================
        // SHORTLIST
        // =========================
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

        // =========================
        // CRUD
        // =========================
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
                .Include(p => p.User)
                .Include(p => p.Category)
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

            // ✅ 1. Đánh dấu xóa mềm
            post.Status = "Deleted";
            post.UpdatedAt = DateTime.Now;

            // ✅ 2. Dọn sạch gợi ý AI liên quan đến bài đăng này
            var targets = _db.AiMatchSuggestions
                .Where(s => s.TargetType == "EmployerPost" && s.TargetId == id);
            _db.AiMatchSuggestions.RemoveRange(targets);

            // ✅ 3. Cập nhật DB
            await _db.SaveChangesAsync();

            return true;
        }


        // =========================
        // Helpers
        // =========================
        private async Task<(float[] Vector, string Hash)> EnsureEmbeddingAsync(string entityType, int entityId, string text)
        {
            if (text.Length > 6000) text = text[..6000];
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

            var embed = await _db.AiEmbeddingStatuses
                .FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId);

            // Nếu content không đổi & đã cache vector -> dùng lại
            if (embed != null && embed.ContentHash == hash && !string.IsNullOrEmpty(embed.VectorData))
            {
                var cached = JsonConvert.DeserializeObject<float[]>(embed.VectorData!)!;
                return (cached, hash);
            }

            // Tạo embedding mới
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
                    VectorData = jsonVec // cần cột NVARCHAR(MAX)
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
                .Where(x => x.Status == "Active")                  // 👈 filter
                .FirstOrDefaultAsync(x => x.JobSeekerPostId == seekerPostId);

                if (seeker == null) continue;


                // Bắt buộc cùng Category (nếu bạn muốn loosy, có thể giảm điều kiện này)
                if (mustMatchCategoryId.HasValue && seeker.CategoryId != mustMatchCategoryId) continue;

                double score = ComputeHybridScore(
                    m.Score, employerLocation, employerTitle, seeker.PreferredLocation, seeker.Title);

                result.Add((seeker, score));
            }

            return result;
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
                        // KHÔNG set UpdatedAt nếu DB chưa có cột
                    });
                }
                else
                {
                    exist.RawScore = score;
                    exist.MatchPercent = (int)Math.Round(score * 100);
                    exist.Reason = "AI cập nhật đề xuất";
                }
            }

            // Xoá những suggestion cũ không còn nằm trong top
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

        // =========================
        // Scoring ưu tiên theo khu vực & miền
        // =========================
        private double ComputeHybridScore(
            double embeddingScore,
            string employerLocation,
            string employerTitle,
            string? seekerLocation,
            string? seekerTitle)
        {
            double locationBonus = 0;
            double titleBonus = 0;
            double penalty = 1.0;

            var eLoc = Normalize(employerLocation);
            var sLoc = Normalize(seekerLocation ?? "");

            // Ưu tiên theo mức độ gần: cùng địa danh -> cùng miền -> xa
            // 1) Cùng địa danh (quận/huyện/tỉnh/thành)
            if (!string.IsNullOrEmpty(eLoc) && !string.IsNullOrEmpty(sLoc))
            {
                if (eLoc == sLoc || eLoc.Contains(sLoc) || sLoc.Contains(eLoc))
                {
                    locationBonus = 0.40; // rất gần
                }
                else
                {
                    // 2) Cùng miền Bắc/Trung/Nam
                    string[] north = { "ha noi", "hà nội", "hai phong", "hải phòng", "bac ninh", "bắc ninh", "bac giang", "bắc giang", "thai nguyen", "thái nguyên" };
                    string[] central = { "da nang", "đà nẵng", "hue", "huế", "quang nam", "quảng nam", "quang ngai", "quảng ngãi" };
                    string[] south = { "ho chi minh", "hồ chí minh", "tp hcm", "tphcm", "binh duong", "bình dương", "dong nai", "đồng nai", "can tho", "cần thơ" };

                    bool eN = north.Any(l => eLoc.Contains(l));
                    bool eC = central.Any(l => eLoc.Contains(l));
                    bool eS = south.Any(l => eLoc.Contains(l));

                    bool sN = north.Any(l => sLoc.Contains(l));
                    bool sC = central.Any(l => sLoc.Contains(l));
                    bool sS = south.Any(l => sLoc.Contains(l));

                    if ((eN && sN) || (eC && sC) || (eS && sS))
                        locationBonus = 0.25; // cùng miền
                    else
                        penalty = 0.60; // khác miền -> phạt mạnh
                }
            }

            // Ưu tiên tiêu đề có chứa nhau
            if (!string.IsNullOrEmpty(seekerTitle) && !string.IsNullOrEmpty(employerTitle))
            {
                var eTitle = employerTitle.ToLowerInvariant();
                var sTitle = seekerTitle.ToLowerInvariant();
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
        // 🧩 Helper: chuyển từ Model -> DTO Out
        // ======================================
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
