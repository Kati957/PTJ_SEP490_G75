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
using PTJ_Service.ImageService;              // ⭐ THÊM
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
        private readonly IImageService _imageService;       // ⭐ THÊM

        public JobSeekerPostService(
            IJobSeekerPostRepository repo,
            JobMatchingDbContext db,
            IAIService ai,
            OpenMapService map,
            LocationDisplayService locDisplay,
            IImageService imageService                  // ⭐ THÊM
        )
            {
            _repo = repo;
            _db = db;
            _ai = ai;
            _map = map;
            _locDisplay = locDisplay;
            _imageService = imageService;              // ⭐ THÊM
            }


        // CREATE
        public async Task<JobSeekerPostResultDto> CreateJobSeekerPostAsync(JobSeekerPostCreateDto dto)
            {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (dto.UserID <= 0)
                throw new Exception("Missing UserID.");

            // 0) VALIDATE SelectedCvId: phải thuộc user + chưa dùng ở post khác
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
                SubCategoryId = dto.SubCategoryId,

                PhoneContact = dto.PhoneContact,
                SelectedCvId = dto.SelectedCvId,   // GẮN CV VÀO BÀI ĐĂNG
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
                };

            await _repo.AddAsync(post);
            await _db.SaveChangesAsync();

            // ⭐⭐⭐ UPLOAD ẢNH (nếu có)
            // UPLOAD ẢNH
            if (dto.Images != null && dto.Images.Any())
                {
                foreach (var file in dto.Images)
                    {
                    var (url, publicId) = await _imageService.UploadImageAsync(file, "JobSeekerPosts");
                    _db.Images.Add(new Image
                        {
                        EntityType = "JobSeekerPost",
                        EntityId = post.JobSeekerPostId,
                        Url = url,
                        PublicId = publicId,
                        Format = file.ContentType,
                        CreatedAt = DateTime.Now
                        });
                    }
                await _db.SaveChangesAsync();
                }


            var freshPost = await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobSeekerPostId == post.JobSeekerPostId);

            if (freshPost == null)
                throw new Exception("Không thể tải lại bài đăng.");

            // 2) SELECTED CV → TẠO EMBEDDING CV (nếu có) (dùng để cache, không chấm điểm riêng)
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

            // 3) EMBEDDING CHO JOB SEEKER POST (gồm CV text nếu có)
            var category = await _db.Categories.FindAsync(freshPost.CategoryId);

            string embedText =
                $"{freshPost.Title}. " +
                $"{freshPost.Description}. " +
                $"Giờ làm: {freshPost.PreferredWorkHours}. " +
                $"Ngành liên quan: {category?.Description ?? category?.Name ?? ""}.";

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

            // 4) TÌM JOB MATCHING
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
                freshPost.SubCategoryId,
                freshPost.PreferredLocation ?? "",
                freshPost.Title,
                freshPost.UserId,
                freshPost.SelectedCvId
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
            var posts = await _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .Include(x => x.SubCategory)
                .Where(x => x.Status == "Active" && x.User.IsActive)
                .ToListAsync();

            return posts.Select(p => new JobSeekerPostDtoOut
                {
                JobSeekerPostId = p.JobSeekerPostId,
                UserID = p.UserId,
                Title = p.Title,
                Description = p.Description,
                PreferredLocation = p.PreferredLocation,
                CategoryName = p.Category?.Name,
                SubCategoryName = p.SubCategory?.Name,
                SeekerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status
                });
            }

        public async Task<IEnumerable<JobSeekerPostDtoOut>> GetByUserAsync(int userId)
            {
            var posts = await _repo.GetByUserAsync(userId);

            // Không bao giờ trả bài Deleted
            posts = posts.Where(x => x.Status != "Deleted");

            return posts.Select(p => new JobSeekerPostDtoOut
                {
                JobSeekerPostId = p.JobSeekerPostId,
                UserID = p.UserId,
                Title = p.Title,
                Description = p.Description,
                PreferredLocation = p.PreferredLocation,
                CategoryName = p.Category?.Name,
                SubCategoryName = p.SubCategory?.Name,
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

            // ❌ Bị Blocked / Inactive / Deleted → không trả về
            if (post.Status == "Blocked" || post.Status == "Inactive" || post.Status == "Deleted")
                return null;

            // ❌ User bị khóa → không trả về
            if (post.User == null || post.User.IsActive == false)
                return null;

            return new JobSeekerPostDtoOut
                {
                JobSeekerPostId = post.JobSeekerPostId,
                UserID = post.UserId,
                Title = post.Title,
                Description = post.Description,
                PreferredLocation = post.PreferredLocation,
                CategoryName = post.Category?.Name,
                SubCategoryName = post.SubCategory?.Name,
                SeekerName = post.User.Username,
                CreatedAt = post.CreatedAt,
                Status = post.Status
                };
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
            post.SubCategoryId = dto.SubCategoryId;
            post.PhoneContact = dto.PhoneContact;
            post.SelectedCvId = dto.SelectedCvId;
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);

            // DELETE OLD IMAGES
            if (dto.DeleteImageIds != null && dto.DeleteImageIds.Any())
                {
                var oldImages = await _db.Images
                    .Where(i => dto.DeleteImageIds.Contains(i.ImageId)
                             && i.EntityType == "JobSeekerPost"
                             && i.EntityId == post.JobSeekerPostId)
                    .ToListAsync();

                foreach (var img in oldImages)
                    {
                    await _imageService.DeleteImageAsync(img.PublicId);
                    _db.Images.Remove(img);
                    }
                }

            // UPLOAD NEW IMAGES
            if (dto.Images != null && dto.Images.Any())
                {
                foreach (var file in dto.Images)
                    {
                    var (url, publicId) = await _imageService.UploadImageAsync(file, "JobSeekerPosts");
                    _db.Images.Add(new Image
                        {
                        EntityType = "JobSeekerPost",
                        EntityId = post.JobSeekerPostId,
                        Url = url,
                        PublicId = publicId,
                        Format = file.ContentType,
                        CreatedAt = DateTime.Now
                        });
                    }
                }

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
                    $"Tóm tắt: {selectedCv.SkillSummary}. " +
                    $"Công việc mong muốn: {selectedCv.PreferredJobType}. " +
                    $"Địa điểm mong muốn: {selectedCv.PreferredLocation}. ";
                }

            var category = await _db.Categories.FindAsync(post.CategoryId);

            string embedText =
                $"{post.Title}. " +
                $"{post.Description}. " +
                $"Giờ làm: {post.PreferredWorkHours}. " +
                $"{cvText} " +
                $"Ngành liên quan: {category?.Description ?? category?.Name ?? ""}.";

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
            // ⭐ Xoá mềm bài đăng
            await _repo.SoftDeleteAsync(id);

            // ⭐ XÓA ẢNH LIÊN QUAN
            var images = await _db.Images
                .Where(i => i.EntityType == "JobSeekerPost" && i.EntityId == id)
                .ToListAsync();

            foreach (var img in images)
                {
                await _imageService.DeleteImageAsync(img.PublicId); // Xóa Cloudinary
                }

            if (images.Any())
                _db.Images.RemoveRange(images);

            // ⭐ XÓA GỢI Ý AI
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
                    $"Tóm tắt: {selectedCv.SkillSummary}. " +
                    $"Công việc mong muốn: {selectedCv.PreferredJobType}. " +
                    $"Địa điểm mong muốn: {selectedCv.PreferredLocation}. ";
                }

            var category = await _db.Categories.FindAsync(post.CategoryId);

            string embedText =
                $"{post.Title}. " +
                $"{post.Description}. " +
                $"Giờ làm: {post.PreferredWorkHours}. " +
                $"{cvText} " +
                $"Ngành liên quan: {category?.Description ?? category?.Name ?? ""}.";

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
                post.SubCategoryId,
                post.PreferredLocation ?? "",
                post.Title ?? "",
                post.UserId,
                post.SelectedCvId
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


        // SCORING – 1 SCORE DUY NHẤT TỪ PINECONE
        private async Task<List<(EmployerPost Job, double Score)>>
            ScoreAndFilterJobsAsync(
                List<(string Id, double Score)> matches,
                int? mustMatchCategoryId,
                int? mustMatchSubCategoryId,
                string preferredLocation,
                string seekerTitle,
                int seekerUserId,
                int? selectedCvId
            )
            {
            var results = new List<(EmployerPost Job, double Score)>();

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

                if (job.User == null || job.User.IsActive == false)
                    continue;

                // Category Filter
                if (mustMatchCategoryId.HasValue &&
                    job.CategoryId != mustMatchCategoryId.Value)
                    continue;

                // SubCategory Filter
                if (mustMatchSubCategoryId.HasValue &&
                    job.SubCategoryId != mustMatchSubCategoryId.Value)
                    continue;

                // Location Filter: chỉ lọc, không cộng điểm
                if (!await IsWithinDistanceAsync(preferredLocation, job.Location))
                    continue;

                // FINAL SCORE = Pinecone similarity
                double finalScore = m.Score;

                results.Add((job, finalScore));
                }

            return results;
            }

        // LOCATION FILTER HELPER – lọc <= 100km, không tính điểm
        private async Task<bool> IsWithinDistanceAsync(string fromLocation, string? toLocation)
            {
            if (string.IsNullOrWhiteSpace(fromLocation) ||
                string.IsNullOrWhiteSpace(toLocation))
                return true; // nếu thiếu info, không loại

            try
                {
                var fromCoord = await _map.GetCoordinatesAsync(fromLocation);
                var toCoord = await _map.GetCoordinatesAsync(toLocation);

                if (fromCoord == null || toCoord == null)
                    return true; // không xác định được thì bỏ qua

                double distanceKm = _map.ComputeDistanceKm(
                    fromCoord.Value.lat, fromCoord.Value.lng,
                    toCoord.Value.lat, toCoord.Value.lng);

                return distanceKm <= 100;
                }
            catch
                {
                return true; // lỗi API thì không loại
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
                    EmployerName = x.EmployerPost.User.Username,
                    x.Note,
                    x.AddedAt
                    }).ToListAsync();
            }


        // Lấy lại danh sách job đã được AI đề xuất
        public async Task<IEnumerable<JobSeekerJobSuggestionDto>> GetSuggestionsByPostAsync(
            int jobSeekerPostId, int take = 10, int skip = 0)
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
                join ep in _db.EmployerPosts.Include(e => e.User)
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
                    Salary = ep.Salary,
                    Location = ep.Location,
                    WorkHours = ep.WorkHours,
                    PhoneContact = ep.PhoneContact,
                    CategoryName = ep.Category != null ? ep.Category.Name : null,
                    SubCategoryName = ep.SubCategory != null ? ep.SubCategory.Name : null,
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
            var sub = await _db.SubCategories.FindAsync(post.SubCategoryId);
            var user = await _db.Users.FindAsync(post.UserId);

            // ⭐ Lấy danh sách ảnh
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

                CategoryName = category?.Name,
                SubCategoryName = sub?.Name,
                SeekerName = user?.Username ?? "",
                CreatedAt = post.CreatedAt,
                Status = post.Status,

                ImageUrls = images      // ⭐ map ra DTO
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

            post.Status = "Inactive";
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
        }
    }
