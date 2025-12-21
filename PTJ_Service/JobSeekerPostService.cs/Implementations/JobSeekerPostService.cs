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
    using PTJ_Service.ImageService;
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
            private readonly IImageService _imageService;

            public JobSeekerPostService(
                IJobSeekerPostRepository repo,
                JobMatchingDbContext db,
                IAIService ai,
                OpenMapService map,
                LocationDisplayService locDisplay,
                IImageService imageService
            )
            {
                _repo = repo;
                _db = db;
                _ai = ai;
                _map = map;
                _locDisplay = locDisplay;
                _imageService = imageService;
            }


        // CREATE
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostCreateDto dto)
            {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
                {
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                if (dto.UserID <= 0)
                    throw new Exception("Missing UserID.");

                JobSeekerCv? selectedCv = null;

                if (dto.SelectedCvId.HasValue)
                    {
                    selectedCv = await _db.JobSeekerCvs
                        .FirstOrDefaultAsync(c => c.Cvid == dto.SelectedCvId.Value &&
                                                  c.JobSeekerId == dto.UserID);

                    if (selectedCv == null)
                        throw new Exception("CV không hợp lệ hoặc không thuộc về người dùng.");

                    bool cvUsed = await _db.JobSeekerPosts
                        .AnyAsync(x =>
                            x.UserId == dto.UserID &&
                            x.SelectedCvId == dto.SelectedCvId &&
                            x.Status != "Deleted");

                    if (cvUsed)
                        throw new Exception("CV này đã được sử dụng cho một bài đăng khác.");
                    }

                var posts = await _db.JobSeekerPosts
                    .Where(x => x.UserId == dto.UserID && x.Status != "Deleted")
                    .ToListAsync();

                int totalPosts = posts.Count;
                bool hasArchived = posts.Any(x => x.Status == "Archived");

                if (totalPosts >= 3)
                    {
                    if (hasArchived)
                        {
                        throw new Exception(
                            "Bạn đã đạt giới hạn 3 bài đăng.\n" +
                            "Hiện bạn có bài đăng đang ở trạng thái 'Đóng'.\n" +
                            "Bạn có thể sửa bài đăng đó và mở lại để sử dụng thay vì tạo bài mới."
                        );
                        }
                    else
                        {
                        throw new Exception(
                            "Bạn đã có 3 bài đăng đang hoạt động. " +
                            "Vui lòng xóa bớt hoặc chuyển bài đăng sang trạng thái đóng trước khi tạo bài mới."
                        );
                        }
                    }

                string fullLocation = await _locDisplay.BuildAddressAsync(
                    dto.ProvinceId, dto.DistrictId, dto.WardId);

                string normalizedSeekerAddress = WithCountry(NormalizeLocation(fullLocation));
                await _map.GetCoordinatesAsync(normalizedSeekerAddress);

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
                    SelectedCvId = dto.SelectedCvId,
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
                    throw new Exception("Không thể tải lại bài đăng.");

                float[] cvEmbedding = Array.Empty<float>();

                if (selectedCv != null)
                    {
                    string cvText = $"{selectedCv.Skills}. {selectedCv.SkillSummary}.";
                                    
                    cvEmbedding = await _ai.CreateEmbeddingAsync(cvText);

                    _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
                        {
                        EntityType = "JobSeekerCV",
                        EntityId = selectedCv.Cvid,
                        ContentHash = Convert.ToHexString(
                            SHA256.HashData(Encoding.UTF8.GetBytes(cvText))),
                        Model = "text-embedding-nomic-embed-text-v2-moe",
                        VectorDim = cvEmbedding.Length,
                        PineconeId = $"JobSeekerCV:{selectedCv.Cvid}",
                        Status = "OK",
                        UpdatedAt = DateTime.Now,
                        VectorData = JsonConvert.SerializeObject(cvEmbedding)
                        });

                    await _db.SaveChangesAsync();
                    }

                var category = await _db.Categories.FindAsync(freshPost.CategoryId);

                string embedText =
                    $"{freshPost.Title}. {freshPost.Description}. Giờ làm: {freshPost.PreferredWorkHours}. ";

                if (selectedCv != null)
                    {
                    embedText += $" | Kỹ năng: {selectedCv.Skills}. {selectedCv.SkillSummary}.";
                    }

                var (vector, hash) = await EnsureEmbeddingAsync(
                    "JobSeekerPost",
                    freshPost.JobSeekerPostId,
                    embedText);

                await _ai.UpsertVectorAsync(
                    ns: "job_seeker_posts",
                    id: $"JobSeekerPost:{freshPost.JobSeekerPostId}",
                    vector: vector,
                    metadata: new
                        {
                        numericPostId = freshPost.JobSeekerPostId,
                        categoryId = freshPost.CategoryId,
                        provinceId = freshPost.ProvinceId,
                        districtId = freshPost.DistrictId,
                        wardId = freshPost.WardId,
                        title = freshPost.Title ?? "",
                        status = freshPost.Status
                        });

                var scored = await ScoreAndFilterJobsAsync(
                    freshPost.JobSeekerPostId,
                    vector,
                    freshPost.CategoryId
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
                            EmployerID = x.Job.UserId,
                            x.Job.Title,
                            x.Job.Description,
                            x.Job.Requirements,
                            SalaryMin = x.Job.SalaryMin,
                            SalaryMax = x.Job.SalaryMax,
                            SalaryType = x.Job.SalaryType,
                            SalaryDisplay = FormatSalary(x.Job.SalaryMin, x.Job.SalaryMax, x.Job.SalaryType),
                            x.Job.Location,
                            x.Job.WorkHours,
                            ExpiredAtText = x.Job.ExpiredAt?.ToString("dd/MM/yyyy"),
                            x.Job.PhoneContact,
                            CategoryName = x.Job.Category?.Name,
                            EmployerName = x.Job.User.Username,
                            IsSaved = savedIds.Contains(x.Job.EmployerPostId)
                            }
                        })
                    .ToList();

                await transaction.CommitAsync();

                return new JobSeekerPostResultDto
                    {
                    Post = await BuildCleanPostDto(freshPost),
                    SuggestedJobs = suggestions
                    };
                }
            catch
                {
                await transaction.RollbackAsync();
                throw;
                }
            }

        // READ

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetAllAsync()
            {
                var posts = await _db.JobSeekerPosts
                    .Include(x => x.User)
                    .ThenInclude(u => u.JobSeekerProfile)
                    .Include(x => x.Category)
                    .Where(x => x.Status == "Active" && x.User.IsActive)
                    .ToListAsync();

                var result = new List<JobSeekerPostDtoOut>();

                foreach (var p in posts)
                {
                    var images = await _db.Images
                        .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == p.JobSeekerPostId)
                        .Select(i => i.Url)
                        .ToListAsync();

                    result.Add(new JobSeekerPostDtoOut
                    {
                        JobSeekerPostId = p.JobSeekerPostId,
                        UserID = p.UserId,
                        Title = p.Title,
                        Description = p.Description,
                        PreferredLocation = p.PreferredLocation,
                        CategoryName = p.Category?.Name,
                        SeekerName = p.User.JobSeekerProfile?.FullName,
                        CreatedAt = p.CreatedAt,
                        Status = p.Status,
                        CvId = p.SelectedCvId,
                    });
                }

                return result;
            }

            public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
            {
                var posts = await _repo.GetByUserAsync(userId);
                posts = posts.Where(x => x.Status != "Deleted");

                var result = new List<JobSeekerPostDtoOut>();

                foreach (var p in posts)
                {
                    var images = await _db.Images
                        .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == p.JobSeekerPostId)
                        .Select(i => i.Url)
                        .ToListAsync();

                    result.Add(new JobSeekerPostDtoOut
                    {
                        JobSeekerPostId = p.JobSeekerPostId,
                        UserID = p.UserId,
                        Title = p.Title,
                        Description = p.Description,
                        PreferredLocation = p.PreferredLocation,
                        CategoryName = p.Category?.Name,
                        SeekerName = p.User.JobSeekerProfile?.FullName,
                        CreatedAt = p.CreatedAt,
                        Status = p.Status,
                        CvId = p.SelectedCvId,
                    });
                }

                return result;
            }

            public async Task<JobSeekerPostDtoOut?> GetByIdAsync(int id)
            {
                var post = await _repo.GetByIdAsync(id);
                if (post == null)
                    return null;

                //  Bị Blocked / Inactive / Deleted → không trả về
                if (post.Status == "Blocked" || post.Status == "Inactive" || post.Status == "Deleted")
                    return null;

                //  User bị khóa → không trả về
                if (post.User == null || post.User.IsActive == false)
                    return null;

                var images = await _db.Images
                .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == post.JobSeekerPostId)
                .Select(i => i.Url)
                .ToListAsync();

                return new JobSeekerPostDtoOut
                {
                    JobSeekerPostId = post.JobSeekerPostId,
                    UserID = post.UserId,
                    Title = post.Title,
                    Description = post.Description,
                    Age = post.Age,
                    Gender = post.Gender,
                    PreferredWorkHours = post.PreferredWorkHours,
                    PreferredWorkHourStart = post.PreferredWorkHours?.Split(" - ").FirstOrDefault(),
                    PreferredWorkHourEnd = post.PreferredWorkHours?.Split(" - ").LastOrDefault(),
                    PreferredLocation = post.PreferredLocation,
                    ProvinceId = post.ProvinceId,
                    DistrictId = post.DistrictId,
                    WardId = post.WardId,
                    PhoneContact = post.PhoneContact,
                    CategoryID = post.CategoryId ?? 0,
                    CategoryName = post.Category?.Name,
                    SeekerName = post.User.JobSeekerProfile?.FullName,
                    CreatedAt = post.CreatedAt,
                    Status = post.Status,
                    CvId = post.SelectedCvId,
                };
            }
            public async Task<IEnumerable<JobSeekerPostDtoOut>> FilterAsync(string status, int? userId, bool isAdmin)
                {
                status = status.ToLower();

                IQueryable<JobSeekerPost> query = _db.JobSeekerPosts
                    .Include(x => x.User)
                    .ThenInclude(u => u.JobSeekerProfile)
                    .Include(x => x.Category);

                switch (status)
                    {
                    case "active":
                        query = query.Where(x =>
                            x.Status == "Active" &&
                            x.User.IsActive);
                        break;

                    case "archived":
                        query = query.Where(x =>
                            x.Status == "Archived" &&
                            (isAdmin || x.UserId == userId));
                        break;

                    case "blocked":
                        if (!isAdmin) return Enumerable.Empty<JobSeekerPostDtoOut>();
                        query = query.Where(x => x.Status == "Blocked");
                        break;

                    case "deleted":
                        if (!isAdmin) return Enumerable.Empty<JobSeekerPostDtoOut>();
                        query = query.Where(x => x.Status == "Deleted");
                        break;

                    default:
                        throw new Exception("Trạng thái không hợp lệ.");
                    }

                var posts = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

                return posts.Select(p => new JobSeekerPostDtoOut
                    {
                    JobSeekerPostId = p.JobSeekerPostId,
                    UserID = p.UserId,
                    Title = p.Title,
                    Description = p.Description,
                    PreferredLocation = p.PreferredLocation,
                    CategoryName = p.Category?.Name,
                    SeekerName = p.User.JobSeekerProfile?.FullName,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status,
                    CvId = p.SelectedCvId
                    });
                }


            // UPDATE

            public async Task<JobSeekerPostDtoOut?> UpdateAsync(int id, JobSeekerPostUpdateDto dto)
            {
                var post = await _repo.GetByIdAsync(id);
                if (post == null || post.Status == "Deleted")
                    return null;

                // VALIDATE SelectedCvId
                if (dto.SelectedCvId.HasValue)
                {
                    var cv = await _db.JobSeekerCvs
                        .FirstOrDefaultAsync(c => c.Cvid == dto.SelectedCvId.Value &&
                                                  c.JobSeekerId == post.UserId);

                    if (cv == null)
                        throw new Exception("CV không hợp lệ hoặc không thuộc về người dùng.");

                    bool cvUsedByOtherPost = await _db.JobSeekerPosts
                        .AnyAsync(x =>
                            x.UserId == post.UserId &&
                            x.SelectedCvId == dto.SelectedCvId &&
                            x.JobSeekerPostId != post.JobSeekerPostId &&
                            x.Status != "Deleted");

                    if (cvUsedByOtherPost)
                        throw new Exception("CV này đang được sử dụng ở bài đăng khác. Hãy xóa bài đăng kia hoặc chọn CV khác.");
                }

                post.Title = dto.Title;
                post.Description = dto.Description;
                post.Age = dto.Age;
                post.Gender = dto.Gender;
                post.PreferredWorkHours = $"{dto.PreferredWorkHourStart} - {dto.PreferredWorkHourEnd}";

                post.ProvinceId = dto.ProvinceId ?? post.ProvinceId;
                post.DistrictId = dto.DistrictId ?? post.DistrictId;
                post.WardId = dto.WardId ?? post.WardId;


                post.PreferredLocation = await _locDisplay.BuildAddressAsync(
                    dto.ProvinceId ?? throw new Exception("ProvinceId is required"),
                    dto.DistrictId ?? throw new Exception("DistrictId is required"),
                    dto.WardId ?? throw new Exception("WardId is required")
                );


                post.CategoryId = dto.CategoryID;
                post.PhoneContact = dto.PhoneContact;
                post.SelectedCvId = dto.SelectedCvId;
                post.UpdatedAt = DateTime.Now;
            string normalizedSeekerAddress = WithCountry(NormalizeLocation(post.PreferredLocation));
            await _map.GetCoordinatesAsync(normalizedSeekerAddress);

            await _repo.UpdateAsync(post);
                await _db.SaveChangesAsync();


                // Lấy CV gắn với bài đăng
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
                        $"Tóm tắt: {selectedCv.SkillSummary}. ";
                }

            var category = await _db.Categories.FindAsync(post.CategoryId);

                string embedText =
                    $"{post.Title}. " +
                    $"{post.Description}. " +
                    $"Giờ làm: {post.PreferredWorkHours}. " +
                    $"{cvText}.";
                

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
                    numericPostId = post.JobSeekerPostId,
                    categoryId = post.CategoryId,
                    provinceId = post.ProvinceId,
                    districtId = post.DistrictId,
                    wardId = post.WardId,
                    title = post.Title ?? "",
                    status = post.Status
                    });


                return await BuildCleanPostDto(post);
            }


            // DELETE (Soft)

            public async Task<bool> DeleteAsync(int id)
            {
                //  Xoá mềm bài đăng
                await _repo.SoftDeleteAsync(id);

                //  XÓA ẢNH LIÊN QUAN
                var images = await _db.Images
                    .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == id)
                    .ToListAsync();

                foreach (var img in images)
                {
                    await _imageService.DeleteImageAsync(img.PublicId); // Xóa Cloudinary
                }

                if (images.Any())
                    _db.Images.RemoveRange(images);

                //  XÓA GỢI Ý AI
                var targets = _db.AiMatchSuggestions
                    .Where(s => (s.SourceType == "JobSeekerPost" && s.SourceId == id)
                             || (s.TargetType == "JobSeekerPost" && s.TargetId == id));

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
                        $"Tóm tắt: {selectedCv.SkillSummary}. ";
                }

            var category = await _db.Categories.FindAsync(post.CategoryId);

                string embedText =
                    $"{post.Title}. " +
                    $"{post.Description}. " +
                    $"Giờ làm: {post.PreferredWorkHours}. " +
                    $"{cvText}. "; 

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
                    numericPostId = post.JobSeekerPostId,
                    categoryId = post.CategoryId,
                    provinceId = post.ProvinceId,
                    districtId = post.DistrictId,
                    wardId = post.WardId,
                    title = post.Title ?? "",
                    status = post.Status
                    });

            string normalizedSeekerAddress = WithCountry(NormalizeLocation(post.PreferredLocation ?? ""));
            await _map.GetCoordinatesAsync(normalizedSeekerAddress);


            var scored = await ScoreAndFilterJobsAsync(
                post.JobSeekerPostId,
                vector,
                post.CategoryId
                );


            await UpsertSuggestionsAsync(
                    "JobSeekerPost",
                    post.JobSeekerPostId,
                    "EmployerPost",
                    scored,
                    keepTop: 5,
                    selectedCvId: post.SelectedCvId
                );

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
                            EmployerID = x.Job.UserId,
                            x.Job.Title,
                            x.Job.Description,
                            x.Job.Requirements,
                            SalaryMin = x.Job.SalaryMin,
                            SalaryMax = x.Job.SalaryMax,
                            SalaryType = x.Job.SalaryType,
                            SalaryDisplay = FormatSalary(x.Job.SalaryMin, x.Job.SalaryMax, x.Job.SalaryType),

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


        // SCORING – 1 SCORE DUY NHẤT TỪ PINECONE
        // SCORING – Query Pinecone chỉ trong các bài Employer đã qua HARD FILTER
           private async Task<List<(EmployerPost Job, double Score)>>
                ScoreAndFilterJobsAsync(
                int seekerPostId,
                float[] seekerVector,
                int? mustMatchCategoryId
                )

            {
            // 1) LỌC CATEGORY
            // Tìm category Other
            var otherCategoryId = await _db.Categories
                    .Where(c => c.Name == "Other" || c.Name == "Khác")
                    .Select(c => (int?)c.CategoryId)
                    .FirstOrDefaultAsync();

                bool isOtherSeeker =
                    otherCategoryId.HasValue &&
                    mustMatchCategoryId.HasValue &&
                    mustMatchCategoryId.Value == otherCategoryId.Value;

                IQueryable<EmployerPost> query = _db.EmployerPosts
                    .Include(x => x.User)
                    .Include(x => x.Category)
                    .Where(x =>
                        x.Status == "Active" &&
                        x.User.IsActive);

                if (otherCategoryId.HasValue)
                    {
                    if (isOtherSeeker)
                        {
                        // Seeker chọn Other → chỉ match job Other
                        query = query.Where(x => x.CategoryId == otherCategoryId.Value);
                        }
                    else
                        {
                        // Seeker không phải Other → loại job Other
                        query = query.Where(x => x.CategoryId != otherCategoryId.Value);
                        }
                    }

                var categoryFiltered = await query.ToListAsync();

                if (!categoryFiltered.Any())
                    return new List<(EmployerPost, double)>();
            // 2) LẤY THÔNG TIN SEEKER (Province/District/Ward)
            var seekerPost = await _db.JobSeekerPosts
                 .AsNoTracking()
                 .FirstOrDefaultAsync(x => x.JobSeekerPostId == seekerPostId);

            if (seekerPost == null)
                return new List<(EmployerPost, double)>();

            // 3) LỌC LOCATION (WARD → DISTRICT → PROVINCE → <=100km)
            var locationPassed = new List<EmployerPost>();

                foreach (var job in categoryFiltered)
                    {
                bool ok = await IsWithinDistanceAsync(
                        seekerProvince: seekerPost.ProvinceId,
                        seekerDistrict: seekerPost.DistrictId,
                        seekerWard: seekerPost.WardId,
                        jobProvince: job.ProvinceId,
                        jobDistrict: job.DistrictId,
                        jobWard: job.WardId,
                        seekerLocation: seekerPost.PreferredLocation ?? "",
                        jobLocation: job.Location ?? ""
                    );


                if (ok)
                        locationPassed.Add(job);
                    }

                if (!locationPassed.Any())
                    return new List<(EmployerPost, double)>();


                // 4) LẤY LIST ID EMPLOYERPOST ĐỂ QUERY PINECONE
                var allowedIds = locationPassed
                    .Select(x => x.EmployerPostId)
                    .ToList();


                // 5) QUERY PINECONE CHỈ TRONG allowedIds
                var pineconeMatches = await _ai.QueryWithIDsAsync(
                    "employer_posts",
                    seekerVector,
                    allowedIds,
                    topK: allowedIds.Count
                );

                if (!pineconeMatches.Any())
                    return new List<(EmployerPost, double)>();


                // 6) GHÉP JOB + SCORE
                var results = new List<(EmployerPost Job, double Score)>();

                foreach (var m in pineconeMatches)
                    {
                    if (!m.Id.StartsWith("EmployerPost:"))
                        continue;

                    if (!int.TryParse(m.Id.Split(':')[1], out int jobId))
                        continue;

                    var job = locationPassed.FirstOrDefault(x => x.EmployerPostId == jobId);
                    if (job != null)
                        results.Add((job, m.Score));
                    }

                return results;
                }

        // LOCATION FILTER – ưu tiên ward → district → province → cuối cùng khoảng cách <= 100km
        private async Task<bool> IsWithinDistanceAsync(
    int seekerProvince,
    int seekerDistrict,
    int seekerWard,
    int jobProvince,
    int jobDistrict,
    int jobWard,
    string seekerLocation,
    string jobLocation
)
            {
            // 1) TRÙNG WARD
            if (seekerWard != 0 && seekerWard == jobWard)
                return true;

            // 2) TRÙNG DISTRICT
            if (seekerDistrict != 0 && seekerDistrict == jobDistrict)
                return true;

            // 3) TRÙNG PROVINCE
            if (seekerProvince != 0 && seekerProvince == jobProvince)
                return true;

            // 4) KHÁC TỈNH → TÍNH KHOẢNG CÁCH <= 300KM
            try
                {
                string seekerAddress = await _locDisplay.BuildAddressAsync(
                    seekerProvince,
                    seekerDistrict,
                    seekerWard
                );

                string jobAddress = await _locDisplay.BuildAddressAsync(
                    jobProvince,
                    jobDistrict,
                    jobWard
                );

                var seekerQuery = WithCountry(NormalizeLocation(seekerAddress));
                var jobQuery = WithCountry(NormalizeLocation(jobAddress));

                var fromCoord = await _map.GetCoordinatesAsync(seekerQuery);
                var toCoord = await _map.GetCoordinatesAsync(jobQuery);

                // fallback an toàn
                if (fromCoord == null || toCoord == null)
                    {
                    return seekerProvince != 0 && seekerProvince == jobProvince;
                    }

                double dist = _map.ComputeDistanceKm(
                    fromCoord.Value.lat, fromCoord.Value.lng,
                    toCoord.Value.lat, toCoord.Value.lng
                );

                return dist <= 300;
                }
            catch (Exception ex)
                {
                Console.WriteLine("Distance check error: " + ex.Message);
                return seekerProvince != 0 && seekerProvince == jobProvince;
                }
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
                     .ThenInclude(u => u.EmployerProfile)
                     .Where(x =>
                         x.JobSeekerId == jobSeekerId &&
                         x.EmployerPost.Status == "Active" &&
                         x.EmployerPost.User.IsActive == true
                     )

                    .Include(x => x.EmployerPost)
                    .ThenInclude(e => e.User)
                    .Where(x => x.JobSeekerId == jobSeekerId)
                    .Select(x => new
                    {
                        x.EmployerPostId,
                        x.EmployerPost.Title,
                        x.EmployerPost.Location,
                        EmployerName = x.EmployerPost.User.EmployerProfile == null
                         ? null
                        : x.EmployerPost.User.EmployerProfile.DisplayName,
                        x.Note,
                        x.AddedAt
                    }).ToListAsync();
            }


            // Lấy lại danh sách job đã được AI đề xuất
            public async Task<IEnumerable<JobSeekerJobSuggestionDto>> GetSuggestionsByPostAsync(
                int jobSeekerPostId, int take = 5, int skip = 0)
            {
                var seekerPost = await _db.JobSeekerPosts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.JobSeekerPostId == jobSeekerPostId);

                if (seekerPost == null)
                    return Enumerable.Empty<JobSeekerJobSuggestionDto>();

                var seekerUserId = seekerPost.UserId;

                var savedJobIds = await _db.JobSeekerShortlistedJobs
                    .Where(x => x.JobSeekerId == seekerUserId)
                    .Select(x => x.EmployerPostId)
                    .ToListAsync();

                var query =
                    from s in _db.AiMatchSuggestions
                    where s.SourceType == "JobSeekerPost"
                       && s.SourceId == jobSeekerPostId
                       && s.TargetType == "EmployerPost"
                    join ep in _db.EmployerPosts.Include(e => e.User).ThenInclude(u => u.EmployerProfile)
                         on s.TargetId equals ep.EmployerPostId
                    where ep.Status == "Active"
                       && ep.User.IsActive == true

                    orderby s.MatchPercent descending, s.RawScore descending, s.CreatedAt descending
                    select new JobSeekerJobSuggestionDto
                    {
                        EmployerPostId = ep.EmployerPostId,
                        EmployerUserId = ep.UserId,
                        Title = ep.Title ?? string.Empty,
                        Description = ep.Description ?? string.Empty,
                        Requirements = ep.Requirements,

                        SalaryMin = ep.SalaryMin,
                        SalaryMax = ep.SalaryMax,
                        SalaryType = ep.SalaryType,
                        SalaryDisplay = FormatSalary(ep.SalaryMin, ep.SalaryMax, ep.SalaryType),

                        Location = ep.Location,
                        WorkHours = ep.WorkHours,

                        ExpiredAtText = ep.ExpiredAt == null
                                        ? null
                                        : ep.ExpiredAt.Value.ToString("dd/MM/yyyy"),

                        PhoneContact = ep.PhoneContact,
                        CategoryName = ep.Category != null ? ep.Category.Name : null,
                        EmployerName = ep.User.EmployerProfile == null
                                         ? ep.User.Username
                                         : ep.User.EmployerProfile.DisplayName,
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
                        Model = "text-embedding-nomic-embed-text-v2-moe",
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

            private async Task<JobSeekerPostDtoOut> BuildCleanPostDto(JobSeekerPostModel post)
            {
                var category = await _db.Categories.FindAsync(post.CategoryId);
                var user = await _db.Users.FindAsync(post.UserId);

                //  Lấy danh sách ảnh
                var images = await _db.Images
                    .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == post.JobSeekerPostId)
                    .Select(i => i.Url)
                    .ToListAsync();

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
                    ProvinceId = post.ProvinceId,
                    DistrictId = post.DistrictId,
                    WardId = post.WardId,
                    CategoryID = post.CategoryId ?? 0,
                    CategoryName = category?.Name,
                    SeekerName = user?.JobSeekerProfile?.FullName ?? "",
                    CreatedAt = post.CreatedAt,
                    Status = post.Status,
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

            private async Task<float[]> GetIndustryEmbeddingAsync(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return Array.Empty<float>();

                if (text.Length > 300)
                    text = text.Substring(0, 300);

                string hash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes("industry:" + text.ToLower()))
                );

                var cache = await _db.AiEmbeddingStatuses
                    .FirstOrDefaultAsync(x => x.EntityType == "Industry" && x.ContentHash == hash);

                if (cache != null && !string.IsNullOrEmpty(cache.VectorData))
                    return JsonConvert.DeserializeObject<float[]>(cache.VectorData)!;

                var vec = await _ai.CreateEmbeddingAsync(text);

                _db.AiEmbeddingStatuses.Add(new AiEmbeddingStatus
                {
                    EntityType = "Industry",
                    EntityId = 0,
                    ContentHash = hash,
                    Model = "text-embedding-3-large",
                    VectorDim = vec.Length,
                    VectorData = JsonConvert.SerializeObject(vec),
                    UpdatedAt = DateTime.Now,
                });

                await _db.SaveChangesAsync();

                return vec;
            }

            // CLOSE (Inactive)
            public async Task<bool> CloseJobSeekerPostAsync(int id)
            {
                var post = await _repo.GetByIdAsync(id);
                if (post == null || post.Status == "Deleted")
                    return false;

                post.Status = "Archived";
                post.UpdatedAt = DateTime.Now;

                await _repo.UpdateAsync(post);
                return true;
            }

            // REOPEN (Active)
            public async Task<bool> ReopenJobSeekerPostAsync(int id)
            {
                var post = await _repo.GetByIdAsync(id);
                if (post == null || post.Status == "Deleted")
                    return false;

                post.Status = "Active";
                post.UpdatedAt = DateTime.Now;

                await _repo.UpdateAsync(post);
                return true;
            }

            private static string FormatSalary(decimal? min, decimal? max, int? type)
                {
                if (min == null && max == null)
                    return "Thỏa thuận";

                string unit = type switch
                    {
                        1 => "/giờ",
                        2 => "/ca",
                        3 => "/ngày",
                        4 => "/tháng",
                        5 => "/dự án",
                        _ => ""
                        };

                if (min != null && max != null)
                    return $"{min:N0} - {max:N0}{unit}";
                if (min != null)
                    return $"Từ {min:N0}{unit}";
                if (max != null)
                    return $"Đến {max:N0}{unit}";

                return "Thỏa thuận";
                }

        private static string NormalizeLocation(string raw)
            {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            raw = raw.Replace("-", ",");
            raw = raw.Replace("Tỉnh ", "");
            raw = raw.Replace("Thành phố ", "");
            raw = raw.Replace("Quận ", "");
            raw = raw.Replace("Huyện ", "");
            raw = raw.Replace("Phường ", "");
            raw = raw.Replace("Xã ", "");
            raw = raw.Replace("Thị trấn ", "");
            raw = raw.Replace("Thị xã ", "");

            var parts = raw
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (parts.Count == 0) return raw;

            // Ưu tiên 3 phần cuối: ward, district, province
            if (parts.Count >= 3)
                return $"{parts[^3]}, {parts[^2]}, {parts[^1]}";

            if (parts.Count == 2)
                return $"{parts[0]}, {parts[1]}";

            return parts[0];
            }

        private static string WithCountry(string address)
            {
            if (string.IsNullOrWhiteSpace(address)) return address;
            // thêm quốc gia để Nominatim dễ parse
            return address.Contains("Vietnam", StringComparison.OrdinalIgnoreCase)
                ? address
                : address + ", Vietnam";
            }

        }
    }
