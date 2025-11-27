using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Models;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;
using PTJ_Service.AiService;
using PTJ_Service.ImageService;
using PTJ_Service.LocationService;
using System.Security.Cryptography;
using System.Text;
using PTJ_Service.Interfaces;
using PTJ_Models.DTO.Notification;

using EmployerPostModel = PTJ_Models.Models.EmployerPost;

namespace PTJ_Service.EmployerPostService.Implementations
{
    public class EmployerPostService : IEmployerPostService
    {
        private readonly IEmployerPostRepository _repo;
        private readonly JobMatchingDbContext _db;
        private readonly IAIService _ai;
        private readonly OpenMapService _map;
        private readonly LocationDisplayService _locDisplay;
        private readonly IImageService _imageService;
        private readonly INotificationService _noti;


        public EmployerPostService(
        IEmployerPostRepository repo,
        JobMatchingDbContext db,
        IAIService ai,
        OpenMapService map,
        LocationDisplayService locDisplay,
        IImageService imageService,
        INotificationService noti
        )
        {
            _repo = repo;
            _db = db;
            _ai = ai;
            _map = map;
            _locDisplay = locDisplay;
            _imageService = imageService;
            _noti = noti;
        }

        // CREATE

        public async Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Dữ liệu không hợp lệ.");

            if (dto.UserID <= 0)
                throw new Exception("Thiếu thông tin UserID khi tạo bài đăng tuyển dụng.");

            // 1️⃣ Check employer exist & active
            var employerUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserID);
            if (employerUser == null)
                throw new Exception("Không tìm thấy tài khoản.");

            if (!employerUser.IsActive)
                throw new Exception("Tài khoản đã bị khóa. Không thể đăng bài tuyển dụng.");

            // 2️⃣ Build location
            string fullLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
            );

            if (!string.IsNullOrWhiteSpace(dto.DetailAddress))
                fullLocation = $"{dto.DetailAddress}, {fullLocation}";

            // 3️⃣ Tạo bài đăng
            var post = new EmployerPostModel
            {
                UserId = dto.UserID,
                Title = dto.Title,
                Description = dto.Description,
                Salary = (!string.IsNullOrEmpty(dto.SalaryText) &&
                          dto.SalaryText.ToLower().Contains("thoả thuận"))
                            ? null
                            : dto.Salary,
                Requirements = dto.Requirements,
                WorkHours = $"{dto.WorkHourStart} - {dto.WorkHourEnd}",
                Location = fullLocation,
                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardId = dto.WardId,
                CategoryId = dto.CategoryID,
                SubCategoryId = dto.SubCategoryId,
                PhoneContact = dto.PhoneContact,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
            };

            await _repo.AddAsync(post);
            await _db.SaveChangesAsync();

            // 4️⃣ Gửi thông báo cho follower của employer
            var followers = await _db.EmployerFollowers
     .Where(f => f.EmployerId == post.UserId && f.IsActive)
     .Select(f => f.JobSeekerId)
     .ToListAsync();

            // Lấy tên employer từ bảng EmployerProfiles
            var employerName = await _db.EmployerProfiles
                .Where(x => x.UserId == post.UserId)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync() ?? "Nhà tuyển dụng";

            if (followers.Any())
            {
                foreach (var jsId in followers)
                {
                    await _noti.SendAsync(new CreateNotificationDto
                    {
                        UserId = jsId,                                
                        NotificationType = "FollowEmployerPosted",
                        RelatedItemId = post.EmployerPostId,
                        Data = new()
            {
                { "EmployerName", employerName },         
                { "PostTitle", post.Title ?? "" }
            }
                    });
                }
            }


            // 5️⃣ Upload ảnh bài đăng
            if (dto.Images != null && dto.Images.Any())
            {
                foreach (var file in dto.Images)
                {
                    var (url, publicId) = await _imageService.UploadImageAsync(file, "EmployerPosts");

                    var img = new Image
                    {
                        EntityType = "EmployerPost",
                        EntityId = post.EmployerPostId,
                        Url = url,
                        PublicId = publicId,
                        Format = file.ContentType,
                        CreatedAt = DateTime.Now
                    };

                    _db.Images.Add(img);
                }
                await _db.SaveChangesAsync();
            }

            // 6️⃣ Load lại bài để tạo embedding
            var freshPost = await _db.EmployerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerPostId == post.EmployerPostId);

            if (freshPost == null)
                throw new Exception("Không thể load lại bài đăng vừa tạo.");

            var category = await _db.Categories.FindAsync(freshPost.CategoryId);

            string embedText =
                $"{freshPost.Title}. " +
                $"{freshPost.Description}. " +
                $"Yêu cầu: {freshPost.Requirements}. " +
                $"Lương: {freshPost.Salary}. " +
                $"Ngành liên quan: {category?.Description ?? category?.Name ?? ""}.";

            var (vector, hash) = await EnsureEmbeddingAsync(
                "EmployerPost",
                freshPost.EmployerPostId,
                embedText
            );

            // 7️⃣ Upsert vector vào Pinecone
            await _ai.UpsertVectorAsync(
                ns: "employer_posts",
                id: $"EmployerPost:{freshPost.EmployerPostId}",
                vector: vector,
                metadata: new
                {
                    title = freshPost.Title ?? "",
                    location = freshPost.Location ?? "",
                    salary = freshPost.Salary ?? 0,
                    categoryId = freshPost.CategoryId ?? 0,
                    postId = freshPost.EmployerPostId
                });

            // 8️⃣ Query ứng viên tương tự
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 100);

            if (!matches.Any())
            {
                _db.AiContentForEmbeddings.Add(new AiContentForEmbedding
                {
                    EntityType = "EmployerPost",
                    EntityId = freshPost.EmployerPostId,
                    Lang = "vi",
                    CanonicalText = $"{freshPost.Title}. {freshPost.Description}. {freshPost.Requirements}. {freshPost.Location}. {freshPost.Salary}",
                    Hash = hash,
                    LastPreparedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();

                return new EmployerPostResultDto
                {
                    Post = await BuildCleanPostDto(freshPost),
                    SuggestedCandidates = new List<AIResultDto>()
                };
            }

            // 9️⃣ Tính điểm
            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                freshPost.CategoryId,
                freshPost.SubCategoryId,
                freshPost.Location ?? "",
                freshPost.Title ?? "",
                freshPost.Requirements ?? ""
            );

            // 1️⃣0️⃣ Lưu gợi ý
            var scoredWithCv = scored.Select(x => (x.Seeker, x.Score, x.CvId)).ToList();

            await UpsertSuggestionsAsync(
                "EmployerPost",
                freshPost.EmployerPostId,
                "JobSeekerPost",
                scoredWithCv,
                keepTop: 5
            );

            // 1️⃣1️⃣ Build danh sách gợi ý trả về
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == freshPost.EmployerPostId)
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
                        SeekerID = x.Seeker.UserId,
                        x.Seeker.Title,
                        x.Seeker.Description,
                        x.Seeker.Age,
                        x.Seeker.Gender,
                        x.Seeker.PreferredLocation,
                        x.Seeker.PreferredWorkHours,
                        x.Seeker.PhoneContact,
                        CategoryName = x.Seeker.Category?.Name,
                        SeekerName = x.Seeker.User.Username,
                        SelectedCvId = x.CvId,
                        IsSaved = savedIds.Contains(x.Seeker.JobSeekerPostId)
                    }
                })
                .ToList();

            return new EmployerPostResultDto
            {
                Post = await BuildCleanPostDto(freshPost),
                SuggestedCandidates = suggestions
            };
        }



        // READ

        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
        {
            // 1️⃣ Lấy danh sách post + user + category + subcategory
            var posts = await _db.EmployerPosts
                .Include(p => p.User).ThenInclude(u => u.EmployerProfile)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Where(p => p.Status == "Active" && p.User.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 2️⃣ Lấy toàn bộ ảnh của các bài đăng bằng 1 query
            var postIds = posts.Select(x => x.EmployerPostId).ToList();

            var imageLookup = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && postIds.Contains(i.EntityId))
                .GroupBy(i => i.EntityId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(i => i.Url).ToList()
                );

            // 3️⃣ Map data ra DTO — không query thêm lần nào nữa
            var result = posts.Select(p => new EmployerPostDtoOut
            {
                EmployerPostId = p.EmployerPostId,
                EmployerId = p.UserId,
                Title = p.Title,
                Description = p.Description,
                Salary = p.Salary,
                Requirements = p.Requirements,
                WorkHours = p.WorkHours,
                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
                SubCategoryName = p.SubCategory?.Name,
                EmployerName = p.User.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = p.CreatedAt,
                Status = p.Status,
                ImageUrls = imageLookup.ContainsKey(p.EmployerPostId)
                            ? imageLookup[p.EmployerPostId]
                            : new List<string>()
            })
            .ToList();

            return result;
        }


        public async Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId)
        {
            // 1️⃣ Lấy bài đăng của employer
            var posts = await _repo.GetByUserAsync(userId);

            // 2️⃣ Loại bỏ Deleted, sắp xếp mới nhất → cũ nhất
            posts = posts
                .Where(x => x.Status != "Deleted")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            // 3️⃣ Lấy toàn bộ ảnh bằng 1 query
            var postIds = posts.Select(x => x.EmployerPostId).ToList();

            var imageLookup = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && postIds.Contains(i.EntityId))
                .GroupBy(i => i.EntityId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(i => i.Url).ToList()
                );

            // 4️⃣ Map ra DTO — không query thêm trong vòng lặp
            var result = posts.Select(p => new EmployerPostDtoOut
            {
                EmployerPostId = p.EmployerPostId,
                EmployerId = p.UserId,
                Title = p.Title,
                Description = p.Description,
                Salary = p.Salary,
                Requirements = p.Requirements,
                WorkHours = p.WorkHours,
                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
                SubCategoryName = p.SubCategory?.Name,
                EmployerName = p.User.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = p.CreatedAt,
                Status = p.Status,

                ImageUrls = imageLookup.ContainsKey(p.EmployerPostId)
                            ? imageLookup[p.EmployerPostId]
                            : new List<string>()
            })
            .ToList();

            return result;
        }


        public async Task<EmployerPostDtoOut?> GetByIdAsync(int id, int? requesterId = null, bool isAdmin = false)
        {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return null;

            var user = post.User;
            bool isOwner = requesterId.HasValue && requesterId == post.UserId;

            // 1️⃣ Employer bị ban → ẩn toàn bộ bài đăng
            if (user == null || !user.IsActive)
            {
                if (!isAdmin)
                    return null;
            }

            // 2️⃣ Blocked → chỉ Admin xem được
            if (post.Status == "Blocked" && !isAdmin)
                return null;

            // 3️⃣ Deleted → chỉ admin xem được
            if (post.Status == "Deleted" && !isAdmin)
                return null;

            // 4️⃣ Archived → chỉ owner (employer) hoặc admin xem được
            if (post.Status == "Archived" && !isOwner && !isAdmin)
                return null;

            // 5️⃣ Load ảnh (1 query)
            var images = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && i.EntityId == post.EmployerPostId)
                .Select(i => i.Url)
                .ToListAsync();

            // 6️⃣ Build DTO
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
                ProvinceId = post.ProvinceId,
                DistrictId = post.DistrictId,
                WardId = post.WardId,
                PhoneContact = post.PhoneContact,
                CategoryName = post.Category?.Name,
                SubCategoryName = post.SubCategory?.Name,
                EmployerName = p.User.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = post.CreatedAt,
                Status = post.Status,
                CompanyLogo = user.EmployerProfile?.AvatarUrl ?? "",
                ImageUrls = images
            };
        }



        // UPDATE

        public async Task<EmployerPostDtoOut?> UpdateAsync(int id, EmployerPostUpdateDto dto, int requesterId, bool isAdmin = false)
        {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return null;

            bool isOwner = post.UserId == requesterId;

            //  Deleted → không ai sửa
            if (post.Status == "Deleted")
                return null;

            //  Blocked → chỉ admin được sửa
            if (post.Status == "Blocked" && !isAdmin)
                return null;

            //  Archived → chỉ owner hoặc admin sửa
            if (post.Status == "Archived" && !isOwner && !isAdmin)
                return null;

            //  Validate địa chỉ
            string fullLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId ?? throw new Exception("ProvinceId is required"),
                dto.DistrictId ?? throw new Exception("DistrictId is required"),
                dto.WardId ?? throw new Exception("WardId is required")
            );

            if (!string.IsNullOrWhiteSpace(dto.DetailAddress))
                fullLocation = $"{dto.DetailAddress}, {fullLocation}";

            //  Update dữ liệu bài post
            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Salary = (!string.IsNullOrEmpty(dto.SalaryText) && dto.SalaryText.Contains("thoả thuận"))
                ? null
                : dto.Salary;

            post.Requirements = dto.Requirements;
            post.Location = fullLocation;

            post.ProvinceId = dto.ProvinceId ?? post.ProvinceId;
            post.DistrictId = dto.DistrictId ?? post.DistrictId;
            post.WardId = dto.WardId ?? post.WardId;

            post.WorkHours = $"{dto.WorkHourStart} - {dto.WorkHourEnd}";
            post.CategoryId = dto.CategoryID;
            post.SubCategoryId = dto.SubCategoryId;
            post.PhoneContact = dto.PhoneContact;
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);

            //  XÓA ẢNH CŨ
            if (dto.DeleteImageIds?.Any() == true)
            {
                var imagesToDelete = await _db.Images
                    .Where(i => dto.DeleteImageIds.Contains(i.ImageId)
                             && i.EntityType == "EmployerPost"
                             && i.EntityId == post.EmployerPostId)
                    .ToListAsync();

                foreach (var img in imagesToDelete)
                {
                    await _imageService.DeleteImageAsync(img.PublicId);
                    _db.Images.Remove(img);
                }
            }

            //  UPLOAD ẢNH MỚI
            if (dto.Images?.Any() == true)
            {
                foreach (var file in dto.Images)
                {
                    var (url, publicId) = await _imageService.UploadImageAsync(file, "EmployerPosts");

                    _db.Images.Add(new Image
                    {
                        EntityType = "EmployerPost",
                        EntityId = post.EmployerPostId,
                        Url = url,
                        PublicId = publicId,
                        Format = file.ContentType,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();

            //  Update embedding và AI
            var category = await _db.Categories.FindAsync(post.CategoryId);

            string embedText = $"{post.Title}. {post.Description}. Yêu cầu: {post.Requirements}. Lương: {post.Salary}. Ngành liên quan: {category?.Description ?? category?.Name}.";

            var (vector, _) = await EnsureEmbeddingAsync("EmployerPost", post.EmployerPostId, embedText);

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

        public async Task<bool> DeleteAsync(int id)
        {
            await _repo.SoftDeleteAsync(id);

            var images = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && i.EntityId == id)
                .ToListAsync();

            foreach (var img in images)
            {
                await _imageService.DeleteImageAsync(img.PublicId);
            }

            if (images.Any())
                _db.Images.RemoveRange(images);

            var targets = _db.AiMatchSuggestions
                .Where(s => s.SourceType == "EmployerPost" && s.SourceId == id
                         || s.TargetType == "EmployerPost" && s.TargetId == id);

            _db.AiMatchSuggestions.RemoveRange(targets);

            await _db.SaveChangesAsync();
            return true;
        }



        // REFRESH

        public async Task<EmployerPostResultDto> RefreshSuggestionsAsync(
     int employerPostId,
     int? requesterId = null,
     bool isAdmin = false)
        {
            var post = await _repo.GetByIdAsync(employerPostId);
            if (post == null)
                throw new Exception("Bài đăng không tồn tại.");

            bool isOwner = requesterId.HasValue && requesterId == post.UserId;

            //  Deleted → không ai refresh
            if (post.Status == "Deleted")
                throw new Exception("Bài đăng đã bị xoá, không thể làm mới đề xuất.");

            //  Blocked → chỉ admin refresh
            if (post.Status == "Blocked" && !isAdmin)
                throw new Exception("Bài đăng này đã bị khoá bởi quản trị viên.");

            //  Archived → chỉ owner hoặc admin
            if (post.Status == "Archived" && !isOwner && !isAdmin)
                throw new Exception("Bạn không có quyền làm mới gợi ý của bài đăng này.");

            //  Lấy category
            var category = await _db.Categories.FindAsync(post.CategoryId);

            //  Build text để embedding
            string embedText =
                $"{post.Title}. {post.Description}. Yêu cầu: {post.Requirements}. " +
                $"Địa điểm: {post.Location}. Lương: {post.Salary}. " +
                $"Ngành liên quan: {category?.Description ?? category?.Name ?? ""}.";

            //  Tạo embedding
            var (vector, _) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
                embedText
            );

            //  Upsert vector vào Pinecone
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

            //  Query ứng viên tương tự (Embedding)
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 100);

            if (!matches.Any())
            {
                return new EmployerPostResultDto
                {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                };
            }

            //  Chấm điểm & lọc ứng viên
            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                post.CategoryId,
                post.SubCategoryId,
                post.Location ?? "",
                post.Title ?? "",
                post.Requirements ?? ""
            );

            //  Upsert top 5 vào DB
            await UpsertSuggestionsAsync(
                "EmployerPost",
                post.EmployerPostId,
                "JobSeekerPost",
                scored,
                keepTop: 5
            );

            //  Lấy danh sách ứng viên đã lưu (saved)
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            //  Build danh sách trả về
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
                        x.Seeker.UserId,
                        x.Seeker.Title,
                        x.Seeker.Description,
                        x.Seeker.Age,
                        x.Seeker.Gender,
                        x.Seeker.PreferredLocation,
                        x.Seeker.PreferredWorkHours,
                        x.Seeker.PhoneContact,
                        CategoryName = x.Seeker.Category?.Name,
                        SeekerName = x.Seeker.User.Username,
                        SelectedCvId = x.CvId,
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


        //  SCORING LOGIC (Category filter + Distance ≤100km + Hybrid score)

        private async Task<List<(JobSeekerPost Seeker, double Score, int? CvId)>>
ScoreAndFilterCandidatesAsync(
    List<(string Id, double Score)> matches,
    int? mustMatchCategoryId,
    int? mustMatchSubCategoryId,
    string employerLocation,
    string employerTitle,
    string employerRequirements)
        {
            var result = new List<(JobSeekerPost, double, int?)>();

            foreach (var m in matches)
            {
                if (!m.Id.StartsWith("JobSeekerPost:"))
                    continue;

                if (!int.TryParse(m.Id.Split(':')[1], out var seekerPostId))
                    continue;

                var seeker = await _db.JobSeekerPosts
                    .Include(x => x.User)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.JobSeekerPostId == seekerPostId);

                if (seeker == null || seeker.Status != "Active")
                    continue;

                // CATEGORY FILTER
                if (mustMatchCategoryId.HasValue &&
                    seeker.CategoryId != mustMatchCategoryId.Value)
                    continue;

                if (mustMatchSubCategoryId.HasValue &&
                    seeker.SubCategoryId != mustMatchSubCategoryId.Value)
                    continue;

                // LOCATION FILTER (chỉ lọc, không tính điểm)
                if (!await IsWithinDistanceAsync(employerLocation, seeker.PreferredLocation))
                    continue;

                //  ONLY 1 SCORE: embedding score
                double finalScore = m.Score;

                result.Add((seeker, finalScore, seeker.SelectedCvId));
            }

            return result;
        }

        private async Task<bool> IsWithinDistanceAsync(string employerLocation, string? seekerLocation)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(employerLocation) &&
                    !string.IsNullOrWhiteSpace(seekerLocation))
                {
                    var employerCoord = await _map.GetCoordinatesAsync(employerLocation);
                    var seekerCoord = await _map.GetCoordinatesAsync(seekerLocation);

                    if (employerCoord != null && seekerCoord != null)
                    {
                        double distanceKm = _map.ComputeDistanceKm(
                            employerCoord.Value.lat, employerCoord.Value.lng,
                            seekerCoord.Value.lat, seekerCoord.Value.lng);

                        return distanceKm <= 100; //  Lọc, không tính điểm
                    }
                }
            }
            catch { }

            return true; // fallback
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
            // Danh sách ứng viên đã được employer "save"
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            // Query thô, KHÔNG xử lý SelectedCvId trong LINQ
            var query =
                from s in _db.AiMatchSuggestions
                where s.SourceType == "EmployerPost"
                   && s.SourceId == employerPostId
                   && s.TargetType == "JobSeekerPost"
                join jsp in _db.JobSeekerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .Include(x => x.SubCategory)
                     on s.TargetId equals jsp.JobSeekerPostId
                where jsp.Status == "Active"
                orderby s.MatchPercent descending, s.RawScore descending, s.CreatedAt descending
                select new
                {
                    Suggest = s,
                    Post = jsp,
                    User = jsp.User,
                    Category = jsp.Category,
                    IsSaved = savedIds.Contains(jsp.JobSeekerPostId)
                };

            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);

            var rawList = await query.ToListAsync();

            //  Xử lý SelectedCvId thủ công sau khi đã có dữ liệu từ SQL
            var result = rawList.Select(x => new EmployerPostSuggestionDto
            {
                JobSeekerPostId = x.Post.JobSeekerPostId,
                SeekerUserId = x.Post.UserId,

                Title = x.Post.Title ?? "",
                Description = x.Post.Description ?? "",
                Age = x.Post.Age,
                Gender = x.Post.Gender,
                PreferredLocation = x.Post.PreferredLocation,
                PreferredWorkHours = x.Post.PreferredWorkHours,
                PhoneContact = x.Post.PhoneContact,
                CategoryName = x.Category?.Name,
                SubCategoryName = x.Post.SubCategory != null ? x.Post.SubCategory.Name : null,
                SeekerName = x.User.Username,

                MatchPercent = x.Suggest.MatchPercent,
                RawScore = Math.Round(x.Suggest.RawScore, 4),
                IsSaved = x.IsSaved,

                CreatedAt = x.Suggest.CreatedAt,
                UpdatedAt = x.Suggest.UpdatedAt,

                SelectedCvId = ParseSelectedCvId(x.Suggest.Reason)
            });

            return result;
        }

        //  Hỗ trợ tách CV=xxx
        private int? ParseSelectedCvId(string? reason)
        {
            if (string.IsNullOrEmpty(reason))
                return null;

            var idx = reason.IndexOf("CV=");
            if (idx == -1)
                return null;

            var num = new string(reason
                .Substring(idx + 3)
                .TakeWhile(char.IsDigit)
                .ToArray());

            return int.TryParse(num, out var id) ? id : null;
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
            var sub = await _db.SubCategories.FindAsync(post.SubCategoryId);
            var images = await _db.Images
    .Where(i => i.EntityType == "EmployerPost" && i.EntityId == post.EmployerPostId)
    .Select(i => i.Url)
    .ToListAsync();

            return new EmployerPostDtoOut
            {
                EmployerPostId = post.EmployerPostId,
                EmployerId = post.UserId,

                Title = post.Title,
                Description = post.Description,
                Salary = post.Salary,
                SalaryText = post.Salary == null ? "Thỏa thuận" : null,

                Requirements = post.Requirements,
                WorkHours = post.WorkHours,

                WorkHourStart = post.WorkHours?.Split('-')[0].Trim(),
                WorkHourEnd = post.WorkHours?.Split('-').Length > 1
                ? post.WorkHours.Split('-')[1].Trim()
                : null,


                Location = post.Location,

                //  TRẢ ĐÚNG VỀ CLIENT
                //ProvinceId = post.ProvinceId,
                //DistrictId = post.DistrictId,
                //WardId = post.WardId,

                PhoneContact = post.PhoneContact,
                CategoryName = category?.Name,
                SubCategoryName = sub?.Name,
                EmployerName = user?.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = post.CreatedAt,
                ImageUrls = images,
                Status = post.Status
            };
        }



        private async Task UpsertSuggestionsAsync(
    string sourceType,
    int sourceId,
    string targetType,
    List<(JobSeekerPost Seeker, double Score, int? CvId)> scored,
    int keepTop)
        {
            // Lấy top N ứng viên (theo điểm)
            var top = scored
                .OrderByDescending(x => x.Score)
                .Take(keepTop)
                .ToList();

            // ID bài ứng viên cần giữ lại (để xoá bớt những cái cũ)
            var keepIds = top
                .Select(t => t.Seeker.JobSeekerPostId)
                .ToHashSet();

            // Ghi vào DB
            foreach (var (seeker, score, cvId) in top)
            {
                var exist = await _db.AiMatchSuggestions.FirstOrDefaultAsync(x =>
                    x.SourceType == sourceType &&
                    x.SourceId == sourceId &&
                    x.TargetType == targetType &&
                    x.TargetId == seeker.JobSeekerPostId);

                string reason = cvId.HasValue
                    ? $"AI đề xuất | CV={cvId}"
                    : "AI đề xuất";

                if (exist == null)
                {
                    // Thêm mới
                    _db.AiMatchSuggestions.Add(new AiMatchSuggestion
                    {
                        SourceType = sourceType,
                        SourceId = sourceId,
                        TargetType = targetType,
                        TargetId = seeker.JobSeekerPostId,
                        RawScore = score,
                        MatchPercent = (int)Math.Round(score * 100),
                        Reason = reason,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    // Update
                    exist.RawScore = score;
                    exist.MatchPercent = (int)Math.Round(score * 100);
                    exist.Reason = reason;
                    exist.UpdatedAt = DateTime.Now;
                }
            }

            // Xoá những đề xuất cũ không nằm trong top N
            var obsolete = await _db.AiMatchSuggestions
                .Where(x =>
                    x.SourceType == sourceType &&
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

        public async Task<string> CloseEmployerPostAsync(int id)
        {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return "Bài đăng không tồn tại.";

            if (post.Status == "Deleted")
                return "Bài đăng đã bị xóa và không thể đóng.";

            if (post.Status == "Archived")
                return "Bài đăng đã ở trạng thái Archived.";

            post.Status = "Archived";
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);
            await _repo.SaveChangesAsync();

            return "Đã đóng bài đăng.";
        }


        public async Task<string> ReopenEmployerPostAsync(int id)
        {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return "Bài đăng không tồn tại.";

            if (post.Status == "Deleted")
                return "Bài đăng đã bị xóa và không thể mở lại.";

            if (post.Status == "Active")
                return "Bài đăng đã ở trạng thái Active.";

            post.Status = "Active";
            post.UpdatedAt = DateTime.Now;

            await _repo.UpdateAsync(post);
            await _repo.SaveChangesAsync();

            return "Đã mở lại bài đăng.";
        }
    }
}