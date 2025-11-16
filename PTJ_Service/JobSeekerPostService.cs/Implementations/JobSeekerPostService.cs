using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Data.Repositories.Interfaces.JPost;
using PTJ_Models;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;
using PTJ_Service.AiService;
using PTJ_Service.JobSeekerPostService;
using PTJ_Service.JobSeekerPostService.cs.Interfaces;
using PTJ_Service.LocationService;
using System.Security.Cryptography;
using System.Text;
using JobSeekerPostModel = PTJ_Models.Models.JobSeekerPost;

namespace PTJ_Service.JobSeekerPostService.Implementations
    {
    public class JobSeekerPostService : IJobSeekerPostService
        {
        private readonly IJobSeekerPostRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;
        private readonly OpenMapService _map;
        private readonly LocationDisplayService _locDisplay;
        public JobSeekerPostService(
            IJobSeekerPostRepository repo,
            JobMatchingDbContext db,
            IAIService ai,
            OpenMapService map,
            LocationDisplayService locDisplay)
            {
            _repo = repo;
            _db = db;
            _ai = ai;
            _map = map;
            _locDisplay = locDisplay;
            }


        // CREATE
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostDto dto)
            {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (dto.UserID <= 0)
                throw new Exception("Missing UserID.");

            // =========================================================
            // 0) VALIDATE SelectedCvId: phải thuộc user + chưa dùng ở post khác
            // =========================================================
            JobSeekerCv? selectedCv = null;

            if (dto.SelectedCvId.HasValue)
                {
                // CV phải thuộc về user này
                selectedCv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == dto.SelectedCvId.Value &&
                                              c.JobSeekerId == dto.UserID);

                if (selectedCv == null)
                    throw new Exception("CV không hợp lệ hoặc không thuộc về người dùng.");

                // CV này đã gắn vào bài đăng nào khác chưa?
                bool cvUsed = await _db.JobSeekerPosts
                    .AnyAsync(x =>
                        x.UserId == dto.UserID &&
                        x.SelectedCvId == dto.SelectedCvId &&
                        x.Status != "Deleted");

                if (cvUsed)
                    throw new Exception("CV này đã được sử dụng cho một bài đăng khác. Vui lòng chọn CV khác hoặc sửa bài đăng cũ.");
                }

            string fullLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
            );

            // 1) Create Post
            var post = new JobSeekerPostModel
                {
                UserId = dto.UserID,
                Title = dto.Title,
                Description = dto.Description,
                Age = dto.Age,
                Gender = dto.Gender,
                PreferredWorkHours = $"{dto.PreferredWorkHourStart} - {dto.PreferredWorkHourEnd}",

                PreferredLocation = fullLocation,

                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardId = dto.WardId,
    
                CategoryId = dto.CategoryID,
                PhoneContact = dto.PhoneContact,
                SelectedCvId = dto.SelectedCvId,   // ✅ GẮN CV VÀO BÀI ĐĂNG
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
                };

            await _repo.AddAsync(post);
            await _db.SaveChangesAsync();

            var freshPost = await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobSeekerPostId == post.JobSeekerPostId);

            if (freshPost == null)
                throw new Exception("Cannot reload post.");

            // =========================================================
            // 2) SELECTED CV → TẠO EMBEDDING CV (nếu có)
            // =========================================================
            float[] cvEmbedding = Array.Empty<float>();

            if (selectedCv != null)
                {
                string cvText =
                    $"{selectedCv.Skills}. {selectedCv.SkillSummary}. {selectedCv.PreferredJobType}. {selectedCv.PreferredLocation}";

                cvEmbedding = await _ai.CreateEmbeddingAsync(cvText);

                _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
                    {
                    EntityType = "JobSeekerCV",
                    EntityId = selectedCv.Cvid,
                    ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cvText))),
                    Model = "text-embedding-3-large",
                    VectorDim = cvEmbedding.Length,
                    PineconeId = $"JobSeekerCV:{selectedCv.Cvid}",
                    Status = "OK",
                    UpdatedAt = DateTime.Now,
                    VectorData = JsonConvert.SerializeObject(cvEmbedding)
                    });

                await _db.SaveChangesAsync();
                }

            // =========================================================
            // 3) EMBEDDING CHO JOB SEEKER POST (gồm CV text nếu có)
            // =========================================================
            string embedText =
                $"{freshPost.Title}. {freshPost.Description}. Giờ làm: {freshPost.PreferredWorkHours}.";

            if (selectedCv != null)
                {
                embedText += $" | CV: {selectedCv.Skills} {selectedCv.SkillSummary} {selectedCv.PreferredJobType}";
                }

            var (vector, hash) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                freshPost.JobSeekerPostId,
                embedText
            );

            await _ai.UpsertVectorAsync(
                ns: "job_seeker_posts",
                id: $"JobSeekerPost:{freshPost.JobSeekerPostId}",
                vector: vector,
                metadata: new
                    {
                    title = freshPost.Title,
                    location = freshPost.PreferredLocation,
                    categoryId = freshPost.CategoryId,
                    postId = freshPost.JobSeekerPostId
                    });

            // =========================================================
            // 4) TÌM JOB MATCHING
            // =========================================================
            var matches = await _ai.QuerySimilarAsync("employer_posts", vector, 100);

            if (!matches.Any())
                {
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                    {
                    EntityType = "JobSeekerPost",
                    EntityId = freshPost.JobSeekerPostId,
                    Lang = "vi",
                    CanonicalText = embedText,
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                    });
                await _db.SaveChangesAsync();

                return new JobSeekerPostResultDto
                    {
                    Post = await BuildCleanPostDto(freshPost),
                    SuggestedJobs = new List<AIResultDto>()
                    };
                }

            var scored = await ScoreAndFilterJobsAsync(
                matches,
                freshPost.CategoryId,
                freshPost.PreferredLocation ?? "",
                freshPost.Title,
                freshPost.UserId,
                freshPost.SelectedCvId         // ✅ TRUYỀN CV CỦA BÀI ĐĂNG
            );

            await UpsertSuggestionsAsync(
                "JobSeekerPost",
                freshPost.JobSeekerPostId,
                "EmployerPost",
                scored,
                keepTop: 5,
                selectedCvId: dto.SelectedCvId
            );

            var savedIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == freshPost.UserId)
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
                        x.Job.Description,
                        x.Job.Requirements,
                        x.Job.Salary,
                        x.Job.Location,
                        x.Job.WorkHours,
                        x.Job.PhoneContact,
                        CategoryName = x.Job.Category?.Name,
                        EmployerName = x.Job.User.Username,
                        IsSaved = savedIds.Contains(x.Job.EmployerPostId)
                        }
                    })
                .ToList();

            return new JobSeekerPostResultDto
                {
                Post = await BuildCleanPostDto(freshPost),
                SuggestedJobs = suggestions
                };
            }


        // READ

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
                UserID = post.UserId,
                Title = post.Title,
                Description = post.Description,
                PreferredLocation = post.PreferredLocation,
                CategoryName = post.Category?.Name,
                SeekerName = post.User.Username,
                CreatedAt = post.CreatedAt,
                Status = post.Status
                };
            }


        // UPDATE

        public async Task<JobSeekerPostDtoOut?> UpdateAsync(int id, JobSeekerPostDto dto)
            {
            var post = await _repo.GetByIdAsync(id);
            if (post == null || post.Status == "Deleted")
                return null;

            // --- VALIDATE SelectedCvId: phải thuộc user + không bị dùng ở post khác ---
            // Kiểm tra SelectedCvId hợp lệ
            if (dto.SelectedCvId.HasValue)
                {
                // 1. CV phải thuộc user
                var cv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == dto.SelectedCvId.Value &&
                                              c.JobSeekerId == post.UserId);

                if (cv == null)
                    throw new Exception("CV không hợp lệ hoặc không thuộc về người dùng.");

                // 2. CV này có đang được dùng ở bài đăng khác hay không?
                bool cvUsedByOtherPost = await _db.JobSeekerPosts
                    .AnyAsync(x =>
                        x.UserId == post.UserId &&
                        x.SelectedCvId == dto.SelectedCvId &&
                        x.JobSeekerPostId != post.JobSeekerPostId &&   // loại trừ chính bài này
                        x.Status != "Deleted");                        // chỉ tính bài chưa xóa

                if (cvUsedByOtherPost)
                    throw new Exception("CV này đang được sử dụng ở bài đăng khác. "
                                        + "Hãy xóa bài đăng kia hoặc chọn CV khác.");
                }


            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Age = dto.Age;
            post.Gender = dto.Gender;
            post.PreferredWorkHours = $"{dto.PreferredWorkHourStart} - {dto.PreferredWorkHourEnd}";

            //UPDATE 3 TRƯỜNG NÀY
            post.ProvinceId = dto.ProvinceId;
            post.DistrictId = dto.DistrictId;
            post.WardId = dto.WardId;

            // Build lại PreferredLocation
            post.PreferredLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
            );

            post.CategoryId = dto.CategoryID;
            post.PhoneContact = dto.PhoneContact;
            post.SelectedCvId = dto.SelectedCvId;
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);


            // 🔍 Lấy CV GẮN VỚI BÀI ĐĂNG (không dùng "CV mới nhất của user")
            JobSeekerCv? selectedCv = null;
            if (post.SelectedCvId.HasValue)
                {
                selectedCv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == post.SelectedCvId.Value &&
                                              c.JobSeekerId == post.UserId);
                }

            string cvText = "";
            if (selectedCv != null)
                {
                cvText =
                    $"Kỹ năng: {selectedCv.Skills}. " +
                    $"Tóm tắt: {selectedCv.SkillSummary}. " +
                    $"Công việc mong muốn: {selectedCv.PreferredJobType}. " +
                    $"Địa điểm mong muốn: {selectedCv.PreferredLocation}. ";
                }

            string embedText =
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}. {cvText}";

            var (vector, _) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                embedText
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




        // DELETE (Soft)

        public async Task<bool> DeleteAsync(int id)
            {
            await _repo.SoftDeleteAsync(id);

            var targets = _db.AiMatchSuggestions
                .Where(s => s.SourceType == "JobSeekerPost" && s.SourceId == id
                         || s.TargetType == "JobSeekerPost" && s.TargetId == id);

            _db.AiMatchSuggestions.RemoveRange(targets);
            await _db.SaveChangesAsync();

            return true;
            }


        // REFRESH SUGGESTIONS

        public async Task<JobSeekerPostResultDto> RefreshSuggestionsAsync(int jobSeekerPostId)
            {
            var post = await _repo.GetByIdAsync(jobSeekerPostId);
            if (post == null)
                throw new Exception("Không tìm thấy bài đăng.");

            // 🔍 Lấy CV gắn với bài đăng (SelectedCvId)
            JobSeekerCv? selectedCv = null;
            if (post.SelectedCvId.HasValue)
                {
                selectedCv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == post.SelectedCvId.Value &&
                                              c.JobSeekerId == post.UserId);
                }

            string cvText = "";
            if (selectedCv != null)
                {
                cvText =
                    $"Kỹ năng: {selectedCv.Skills}. " +
                    $"Tóm tắt: {selectedCv.SkillSummary}. " +
                    $"Công việc mong muốn: {selectedCv.PreferredJobType}. " +
                    $"Địa điểm mong muốn: {selectedCv.PreferredLocation}. ";
                }

            string embedText =
                $"{post.Title}. {post.Description}. Giờ làm: {post.PreferredWorkHours}. {cvText}";

            var (vector, _) = await EnsureEmbeddingAsync(
                "JobSeekerPost",
                post.JobSeekerPostId,
                embedText
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

            var scored = await ScoreAndFilterJobsAsync(
                matches,
                post.CategoryId,
                post.PreferredLocation ?? "",
                post.Title ?? "",
                post.UserId,
                post.SelectedCvId       // ✅ DÙNG CV CỦA BÀI
            );

            await UpsertSuggestionsAsync("JobSeekerPost", post.JobSeekerPostId, "EmployerPost", scored, keepTop: 5, selectedCvId: post.SelectedCvId);

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
                        x.Job.Description,
                        x.Job.Requirements,
                        x.Job.Salary,
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



        // SCORING (Hybrid)
        private async Task<List<(EmployerPost Job, double Score)>>
    ScoreAndFilterJobsAsync(
        List<(string Id, double Score)> matches,
        int? mustMatchCategoryId,
        string preferredLocation,
        string seekerTitle,
        int seekerUserId,
        int? selectedCvId
    )
            {
            var results = new List<(EmployerPost Job, double Score)>();

            // Lấy embedding của CV nếu có
            float[] cvEmbedding = Array.Empty<float>();
            JobSeekerCv? cv = null;

            if (selectedCvId.HasValue)
                {
                cv = await _db.JobSeekerCvs
                    .FirstOrDefaultAsync(c => c.Cvid == selectedCvId.Value &&
                                              c.JobSeekerId == seekerUserId);

                if (cv != null)
                    {
                    var embedRecord = await _db.AiEmbeddingStatuses
                        .FirstOrDefaultAsync(e =>
                            e.EntityType == "JobSeekerCV" &&
                            e.EntityId == cv.Cvid);

                    if (embedRecord != null && embedRecord.VectorData != null)
                        cvEmbedding = JsonConvert.DeserializeObject<float[]>(embedRecord.VectorData);
                    }
                }

            foreach (var m in matches)
                {
                if (!m.Id.StartsWith("EmployerPost:"))
                    continue;

                if (!int.TryParse(m.Id.Split(':')[1], out int employerPostId))
                    continue;

                var job = await _db.EmployerPosts
                    .Include(x => x.User)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.EmployerPostId == employerPostId);

                if (job == null || job.Status != "Active")
                    continue;

                // CATEGORY FILTER
                if (mustMatchCategoryId.HasValue &&
                    job.CategoryId != mustMatchCategoryId.Value)
                    continue;

                // LOCATION SCORE
                double locationScore = await ComputeHybridScoreAsync(
                    m.Score,
                    preferredLocation,
                    job.Location
                );

                double finalScore = locationScore;

                // === CV BOOST (nếu có) ===
                if (cv != null && cvEmbedding.Length > 0)
                    {
                    float[] jdEmbedding = await _ai.CreateEmbeddingAsync(
                        $"{job.Title}. {job.Requirements}. {job.Description}"
                    );

                    if (jdEmbedding.Length == cvEmbedding.Length)
                        {
                        double cvCosine = Cosine(cvEmbedding, jdEmbedding);
                        finalScore += cvCosine * 0.35;
                        }

                    // === Skill Boost ===
                    var cvSkills = (cv.Skills ?? "")
                        .ToLower()
                        .Split(',', ';', '.', ' ')
                        .Where(s => s.Length > 1)
                        .Distinct()
                        .ToList();

                    var jdReq = (job.Requirements ?? "")
                        .ToLower()
                        .Split(',', ';', '.', ' ')
                        .Where(s => s.Length > 1)
                        .Distinct()
                        .ToList();

                    int matched = cvSkills.Count(s => jdReq.Contains(s));
                    if (cvSkills.Count > 0)
                        {
                        double overlap = (double)matched / cvSkills.Count;
                        finalScore += overlap * 0.25;
                        }
                    }

                finalScore = Math.Clamp(finalScore, 0, 1);

                results.Add((job, finalScore));
                }

            return results;
            }


        private double Cosine(float[] a, float[] b)
            {
            if (a == null || b == null)
                return 0;
            if (a.Length != b.Length)
                return 0;

            double dot = 0;
            double magA = 0;
            double magB = 0;

            for (int i = 0; i < a.Length; i++)
                {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
                }

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
            }




        private async Task<double> ComputeHybridScoreAsync(
     double contentMatchScore,
     string seekerLocation,
     string? jobLocation)
            {
            // === Trọng số ===
            const double WEIGHT_CONTENT_MATCH = 0.7;      // mức độ phù hợp về nội dung (JD ↔ CV)
            const double WEIGHT_DISTANCE_FACTOR = 0.3;    // mức độ gần về vị trí

            double locationMatchScore = 0.5; // trung lập nếu không xác định được

            try
                {
                if (!string.IsNullOrWhiteSpace(seekerLocation) && !string.IsNullOrWhiteSpace(jobLocation))
                    {
                    var seekerCoord = await _map.GetCoordinatesAsync(seekerLocation);
                    var jobCoord = await _map.GetCoordinatesAsync(jobLocation);

                    if (seekerCoord != null && jobCoord != null)
                        {
                        double distanceKm = _map.ComputeDistanceKm(
                            seekerCoord.Value.lat, seekerCoord.Value.lng,
                            jobCoord.Value.lat, jobCoord.Value.lng);

                        // === Điểm vị trí: càng gần càng cao ===
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

            // === Điểm tổng hợp ===
            double totalMatchScore =
                (contentMatchScore * WEIGHT_CONTENT_MATCH) +
                (locationMatchScore * WEIGHT_DISTANCE_FACTOR);

            return Math.Clamp(totalMatchScore, 0, 1);
            }



        // SHORTLIST

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


        // Lấy lại danh sách job đã được AI đề xuất (đã lưu trong AiMatchSuggestions)
        // cho một JobSeekerPost cụ thể, trả về DTO dễ đọc cho UI.

        public async Task<IEnumerable<JobSeekerJobSuggestionDto>> GetSuggestionsByPostAsync(
            int jobSeekerPostId, int take = 10, int skip = 0)
            {
            // 1) Lấy JobSeekerPost để biết userId (dùng check IsSaved)
            var seekerPost = await _db.JobSeekerPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobSeekerPostId == jobSeekerPostId);

            if (seekerPost == null)
                return Enumerable.Empty<JobSeekerJobSuggestionDto>();

            var seekerUserId = seekerPost.UserId;

            // 2) Lấy danh sách job mà ứng viên (userId) đã lưu -> đánh dấu IsSaved
            var savedJobIds = await _db.JobSeekerShortlistedJobs
                .Where(x => x.JobSeekerId == seekerUserId)
                .Select(x => x.EmployerPostId)
                .ToListAsync();

            // 3) Đọc gợi ý đã cache trong bảng AiMatchSuggestions
            var query =
                from s in _db.AiMatchSuggestions
                where s.SourceType == "JobSeekerPost"
                   && s.SourceId == jobSeekerPostId
                   && s.TargetType == "EmployerPost"
                join ep in _db.EmployerPosts.Include(e => e.User)
                     on s.TargetId equals ep.EmployerPostId
                where ep.Status == "Active" // chỉ lấy job còn active
                orderby s.MatchPercent descending, s.RawScore descending, s.CreatedAt descending
                select new JobSeekerJobSuggestionDto
                    {
                    EmployerPostId = ep.EmployerPostId,
                    EmployerUserId = ep.UserId,

                    Title = ep.Title ?? string.Empty,
                    Description = ep.Description ?? string.Empty,
                    Requirements = ep.Requirements,
                    Salary = ep.Salary,
                    Location = ep.Location,
                    WorkHours = ep.WorkHours,
                    PhoneContact = ep.PhoneContact,
                    CategoryName = ep.Category != null ? ep.Category.Name : null,

                    EmployerName = ep.User.Username,

                    MatchPercent = s.MatchPercent,
                    RawScore = Math.Round(s.RawScore, 4),

                    IsSaved = savedJobIds.Contains(ep.EmployerPostId),

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

        private async Task<JobSeekerPostDtoOut> BuildCleanPostDto(JobSeekerPostModel post)
            {
            var category = await _db.Categories.FindAsync(post.CategoryId);
            var user = await _db.Users.FindAsync(post.UserId);

            return new JobSeekerPostDtoOut
                {
                JobSeekerPostId = post.JobSeekerPostId,
                UserID = post.UserId,

                Title = post.Title,
                Description = post.Description,
                PreferredLocation = post.PreferredLocation,

                PreferredWorkHours = post.PreferredWorkHours,

                PreferredWorkHourStart = post.PreferredWorkHours?.Split('-')[0].Trim(),
                PreferredWorkHourEnd = post.PreferredWorkHours?.Split('-').Length > 1
                            ? post.PreferredWorkHours.Split('-')[1].Trim()
                            : null,

                //ProvinceId = post.ProvinceId,
                //DistrictId = post.DistrictId,
                //WardId = post.WardId,

                CategoryName = category?.Name,
                SeekerName = user?.Username ?? "",
                CreatedAt = post.CreatedAt,
                Status = post.Status
                };

            }



        private async Task UpsertSuggestionsAsync(
    string sourceType,
    int sourceId,
    string targetType,
    List<(EmployerPost Job, double Score)> scored,
    int keepTop,
    int? selectedCvId = null
)
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
                        Reason = selectedCvId.HasValue
                            ? $"AI đề xuất | CV={selectedCvId}"
                            : "AI đề xuất",
                        CreatedAt = DateTime.Now
                        });
                    }
                else
                    {
                    exist.RawScore = score;
                    exist.MatchPercent = (int)Math.Round(score * 100);
                    exist.Reason = selectedCvId.HasValue
                        ? $"AI cập nhật | CV={selectedCvId}"
                        : "AI cập nhật";
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
