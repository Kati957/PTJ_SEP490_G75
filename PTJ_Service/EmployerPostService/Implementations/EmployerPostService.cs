using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Models;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;
using PTJ_Service.AiService.Interfaces;
using PTJ_Service.LocationService;
using System.Security.Cryptography;
using System.Text;
using EmployerPostModel = PTJ_Models.Models.EmployerPost;

namespace PTJ_Service.EmployerPostService.Implementations
    {
    public class EmployerPostService : IEmployerPostService
        {
        private readonly IEmployerPostRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;
        private readonly OpenMapService _map;

        public EmployerPostService(
            IEmployerPostRepository repo,
            JobMatchingDbContext db,
            IAIService ai,
            OpenMapService map)
            {
            _repo = repo;
            _db = db;
            _ai = ai;
            _map = map;
            }

        // CREATE

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

            await _repo.AddAsync(post);

            //  Quan trọng: đảm bảo có ID thật trước khi tạo embedding
            await _db.SaveChangesAsync();

            //// Embedding nội dung (không nhồi location)
            //var (vector, hash) = await EnsureEmbeddingAsync(
            //    "EmployerPost",
            //    post.EmployerPostId,
            //    $"{dto.Title}. {dto.Description}. Yêu cầu: {dto.Requirements}. Lương: {dto.Salary}"
            //);

            //await _ai.UpsertVectorAsync(
            //    ns: "employer_posts",
            //    id: $"EmployerPost:{post.EmployerPostId}",
            //    vector: vector,
            //    metadata: new
            //        {
            //        title = post.Title ?? "",
            //        location = post.Location ?? "",
            //        salary = post.Salary ?? 0,
            //        categoryId = post.CategoryId ?? 0,
            //        postId = post.EmployerPostId
            //        });

            //// Query rộng để không bỏ sót (topK=100)
            //var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 100);

            //if (!matches.Any())
            //    {
            //    // Pending nếu chưa có kết quả
            //    _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
            //        {
            //        EntityType = "EmployerPost",
            //        EntityId = post.EmployerPostId,
            //        Lang = "vi",
            //        CanonicalText = $"{dto.Title}. {dto.Description}. {dto.Requirements}. {dto.Location}. {dto.Salary}",
            //        Hash = hash,
            //        LastPreparedAt = DateTime.Now
            //        });
            //    await _db.SaveChangesAsync();

            //    return new EmployerPostResultDto
            //        {
            //        Post = await BuildCleanPostDto(post),
            //        SuggestedCandidates = new List<AIResultDto>()
            //        };
            //    }

            //var scored = await ScoreAndFilterCandidatesAsync(
            //    matches,
            //    mustMatchCategoryId: post.CategoryId,
            //    employerLocation: post.Location ?? "",
            //    employerTitle: post.Title ?? ""
            //);

            //await UpsertSuggestionsAsync("EmployerPost", post.EmployerPostId, "JobSeekerPost", scored, keepTop: 5);

            //var savedIds = await _db.EmployerShortlistedCandidates
            //    .Where(x => x.EmployerPostId == post.EmployerPostId)
            //    .Select(x => x.JobSeekerId)
            //    .ToListAsync();

            //var suggestions = scored
            //    .OrderByDescending(x => x.Score)
            //    .Take(5)
            //    .Select(x => new AIResultDto
            //        {
            //        Id = $"JobSeekerPost:{x.Seeker.JobSeekerPostId}",
            //        Score = Math.Round(x.Score * 100, 2),
            //        ExtraInfo = new
            //            {
            //            x.Seeker.JobSeekerPostId,
            //            x.Seeker.Title,
            //            x.Seeker.PreferredLocation,
            //            x.Seeker.PreferredWorkHours,
            //            SeekerName = x.Seeker.User.Username,
            //            IsSaved = savedIds.Contains(x.Seeker.JobSeekerPostId)
            //            }
            //        })
            //    .ToList();

            return new EmployerPostResultDto
                {
                Post = await BuildCleanPostDto(post),
                //SuggestedCandidates = suggestions
                };
            }


        // READ

        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
            {
            var posts = await _repo.GetAllAsync();
            return posts.Select(p => new EmployerPostDtoOut
                {
                EmployerPostId = p.EmployerPostId,
                Title = p.Title,
                Description = p.Description,
                Salary = p.Salary,
                Requirements = p.Requirements,
                WorkHours = p.WorkHours,
                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
                EmployerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status,
                ProfileImgs = p.User.EmployerProfile != null ? p.User.EmployerProfile.AvatarUrl : string.Empty,
                EmployerId = p.UserId,
            });
            }

        public async Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId)
            {
            var posts = await _repo.GetByUserAsync(userId);
            return posts.Select(p => new EmployerPostDtoOut
                {
                EmployerPostId = p.EmployerPostId,
                Title = p.Title,
                Description = p.Description,
                Salary = p.Salary,
                Requirements = p.Requirements,
                WorkHours = p.WorkHours,
                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
                EmployerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status
                });
            }

        public async Task<EmployerPostDtoOut?> GetByIdAsync(int id)
            {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return null;

            return new EmployerPostDtoOut
                {
                EmployerPostId = post.EmployerPostId,
                EmployerId = post.UserId,
                Title = post.Title,
                Description = post.Description,
                Salary = post.Salary,
                Requirements = post.Requirements,
                WorkHours = post.WorkHours,
                Location = post.Location,
                PhoneContact = post.PhoneContact,
                CategoryName = post.Category?.Name,
                EmployerName = post.User.Username,
                CreatedAt = post.CreatedAt,
                Status = post.Status
                };
            }


        // UPDATE

        public async Task<EmployerPostDtoOut?> UpdateAsync(int id, EmployerPostDto dto)
            {
            var post = await _repo.GetByIdAsync(id);
            if (post == null || post.Status == "Deleted")
                return null;

            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Salary = dto.Salary;
            post.Requirements = dto.Requirements;
            post.WorkHours = dto.WorkHours;
            post.Location = dto.Location;
            post.CategoryId = dto.CategoryID;
            post.PhoneContact = dto.PhoneContact;
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);

            var (vector, _) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
                $"{post.Title}. {post.Description}. Yêu cầu: {post.Requirements}. Lương: {post.Salary}"
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
                    categoryId = post.CategoryId ?? 0,
                    postId = post.EmployerPostId
                    });

            return await BuildCleanPostDto(post);
            }


        // DELETE (Soft)

        public async Task<bool> DeleteAsync(int id)
            {
            await _repo.SoftDeleteAsync(id);

            var targets = _db.AiMatchSuggestions
                .Where(s => s.SourceType == "EmployerPost" && s.SourceId == id
                         || s.TargetType == "EmployerPost" && s.TargetId == id);

            _db.AiMatchSuggestions.RemoveRange(targets);
            await _db.SaveChangesAsync();

            return true;
            }


        // REFRESH

        public async Task<EmployerPostResultDto> RefreshSuggestionsAsync(int employerPostId)
            {
            var post = await _repo.GetByIdAsync(employerPostId);
            if (post == null)
                throw new Exception("Bài đăng không tồn tại.");

            var (vector, _) = await EnsureEmbeddingAsync(
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
                    categoryId = post.CategoryId ?? 0,
                    postId = post.EmployerPostId
                    });

            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 100);
            if (!matches.Any())
                {
                return new EmployerPostResultDto
                    {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                    };
                }

            var scored = await ScoreAndFilterCandidatesAsync(matches, post.CategoryId, post.Location ?? "", post.Title ?? "");
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


        // SCORING

        private async Task<List<(JobSeekerPost Seeker, double Score)>> ScoreAndFilterCandidatesAsync(
            List<(string Id, double Score)> matches,
            int? mustMatchCategoryId,
            string employerLocation,
            string employerTitle)
            {
            var result = new List<(JobSeekerPost, double)>();

            foreach (var m in matches)
                {
                if (!m.Id.StartsWith("JobSeekerPost:"))
                    continue;
                if (!int.TryParse(m.Id.Split(':')[1], out var seekerPostId))
                    continue;

                var seeker = await _db.JobSeekerPosts
                    .Include(x => x.User)
                    .Where(x => x.Status == "Active")
                    .FirstOrDefaultAsync(x => x.JobSeekerPostId == seekerPostId);

                if (seeker == null)
                    continue;

                if (mustMatchCategoryId.HasValue && seeker.CategoryId != mustMatchCategoryId)
                    continue;

                double score = await ComputeHybridScoreAsync(
     m.Score, employerLocation, seeker.PreferredLocation);


                result.Add((seeker, score));
                }

            return result;
            }

        private async Task<double> ComputeHybridScoreAsync(
    double contentMatchScore,
    string employerLocation,
    string? seekerLocation)
            {
            // === Trọng số ảnh hưởng ===
            const double WEIGHT_CONTENT_MATCH = 0.7;      // mức độ phù hợp nội dung
            const double WEIGHT_DISTANCE_FACTOR = 0.3;    // mức độ gần về vị trí

            double locationMatchScore = 0.5; // giá trị trung lập nếu không xác định được

            try
                {
                if (!string.IsNullOrWhiteSpace(employerLocation) && !string.IsNullOrWhiteSpace(seekerLocation))
                    {
                    var employerCoord = await _map.GetCoordinatesAsync(employerLocation);
                    var seekerCoord = await _map.GetCoordinatesAsync(seekerLocation);

                    if (employerCoord != null && seekerCoord != null)
                        {
                        double distanceKm = _map.ComputeDistanceKm(
                            employerCoord.Value.lat, employerCoord.Value.lng,
                            seekerCoord.Value.lat, seekerCoord.Value.lng);

                        // === Điểm vị trí càng gần càng cao ===
                        // 0 km → 1.0, 30 km → 0.6, 100 km → 0.0
                        if (distanceKm <= 2)
                            locationMatchScore = 1.0;
                        else if (distanceKm <= 10)
                            locationMatchScore = 0.9;
                        else if (distanceKm <= 30)
                            locationMatchScore = 0.6;
                        else if (distanceKm <= 50)
                            locationMatchScore = 0.3;
                        else if (distanceKm <= 100)
                            locationMatchScore = 0.1;
                        else
                            locationMatchScore = 0.0; // quá xa → loại
                        }
                    }
                }
            catch
                {
                locationMatchScore = 0.5; // fallback nếu API lỗi
                }

            // === Tính điểm tổng hợp ===
            double totalMatchScore =
                (contentMatchScore * WEIGHT_CONTENT_MATCH) +
                (locationMatchScore * WEIGHT_DISTANCE_FACTOR);

            return Math.Clamp(totalMatchScore, 0, 1);
            }



        // SHORTLIST

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

        public async Task<IEnumerable<EmployerPostSuggestionDto>> GetSuggestionsByPostAsync(
           int employerPostId, int take = 10, int skip = 0)
            {
            // Danh sách ứng viên đã được employer "save" (shortlist) cho post này
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            // Lấy gợi ý đã cache trong AiMatchSuggestions
            var query =
                from s in _db.AiMatchSuggestions
                where s.SourceType == "EmployerPost"
                   && s.SourceId == employerPostId
                   && s.TargetType == "JobSeekerPost"
                join jsp in _db.JobSeekerPosts.Include(x => x.User)
                     on s.TargetId equals jsp.JobSeekerPostId
                where jsp.Status == "Active" // chỉ lấy bài seeker còn active
                orderby s.MatchPercent descending, s.RawScore descending, s.CreatedAt descending
                select new EmployerPostSuggestionDto
                    {
                    JobSeekerPostId = jsp.JobSeekerPostId,
                    Title = jsp.Title ?? string.Empty,
                    PreferredLocation = jsp.PreferredLocation,
                    PreferredWorkHours = jsp.PreferredWorkHours,
                    SeekerName = jsp.User.Username,
                    MatchPercent = s.MatchPercent,
                    RawScore = Math.Round(s.RawScore, 4),
                    IsSaved = savedIds.Contains(jsp.JobSeekerPostId),
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                    };

            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);

            return await query.ToListAsync();
            }

        // HELPERS

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
                    exist.UpdatedAt = DateTime.Now;
                    }
                }

            var obsolete = await _db.AiMatchSuggestions
                .Where(x => x.SourceType == sourceType && x.SourceId == sourceId && x.TargetType == targetType && !keepIds.Contains(x.TargetId))
                .ToListAsync();

            if (obsolete.Any())
                _db.AiMatchSuggestions.RemoveRange(obsolete);

            await _db.SaveChangesAsync();
            }
        }
    }
