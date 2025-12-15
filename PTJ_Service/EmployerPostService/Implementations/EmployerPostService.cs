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
using Microsoft.Extensions.Hosting;

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
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
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

                // 1️⃣ Auto Free Subscription
                var sub = await EnsureFreeSubscriptionAsync(dto.UserID);

                // 2️⃣ Nếu user đã mua gói → ưu tiên gói trả phí
                var paidSub = await _db.EmployerSubscriptions
                    .Where(s => s.UserId == dto.UserID && s.Status == "Active" && s.PlanId != 1) // != Free
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

                if (paidSub != null)
                    sub = paidSub;

                // 3️⃣ Kiểm tra hạn & lượt
                if (sub.EndDate != null && sub.EndDate < DateTime.Now)
                    throw new Exception("Gói đăng bài đã hết hạn.");

                if (sub.RemainingPosts <= 0)
                    throw new Exception("Bạn đã hết lượt đăng bài.");

                sub.RemainingPosts--;
                sub.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();

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

                    SalaryMin = dto.SalaryMin,
                    SalaryMax = dto.SalaryMax,
                    SalaryType = dto.SalaryType,

                    Requirements = dto.Requirements,
                    WorkHours = $"{dto.WorkHourStart} - {dto.WorkHourEnd}",
                    ExpiredAt = ParseDate(dto.ExpiredAt),
                    Location = fullLocation,
                    ProvinceId = dto.ProvinceId,
                    DistrictId = dto.DistrictId,
                    WardId = dto.WardId,
                    CategoryId = dto.CategoryID,
                    PhoneContact = dto.PhoneContact,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Status = "Active"
                    };

                await _repo.AddAsync(post);

                // --- Validate Salary ---
                if (post.SalaryMin == null && post.SalaryMax == null && post.SalaryType == null)
                    {
                    // Thỏa thuận → giữ nguyên null
                    }
                else
                    {
                    if (post.SalaryMin < 0)
                        throw new Exception("SalaryMin không hợp lệ.");

                    if (post.SalaryMax < post.SalaryMin)
                        throw new Exception("SalaryMax phải ≥ SalaryMin.");

                    if (post.SalaryType == null)
                        throw new Exception("SalaryType là bắt buộc.");
                    }

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

                // Realtime expiry check
                if (freshPost.ExpiredAt != null && freshPost.ExpiredAt.Value.Date < DateTime.Today)
                    {
                    await transaction.CommitAsync(); // COMMIT trước khi return
                    return new EmployerPostResultDto
                        {
                        Post = await BuildCleanPostDto(freshPost),
                        SuggestedCandidates = new List<AIResultDto>()
                        };
                    }

                var category = await _db.Categories.FindAsync(freshPost.CategoryId);

                string embedText =
                    $@"Tiêu đề: {freshPost.Title}
            Mô tả công việc: {freshPost.Description}
            Yêu cầu ứng viên: {freshPost.Requirements}
            Ngành nghề: {category?.Name}
            Giờ làm việc: {freshPost.WorkHours}";

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
                        numericPostId = freshPost.EmployerPostId,
                        categoryId = freshPost.CategoryId,
                        provinceId = freshPost.ProvinceId,
                        districtId = freshPost.DistrictId,
                        wardId = freshPost.WardId,
                        title = freshPost.Title ?? "",
                        status = freshPost.Status,
                        });

                // 9️⃣ Tính điểm
                var scored = await ScoreAndFilterCandidatesAsync(vector, freshPost);

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

                await transaction.CommitAsync();

                return new EmployerPostResultDto
                    {
                    Post = await BuildCleanPostDto(freshPost),
                    SuggestedCandidates = suggestions
                    };
                }
            catch
                {
                await transaction.RollbackAsync();
                throw;
                }
            }

        // READ

        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
        {
            // 1️⃣ Lấy danh sách post + user + category + subcategory
            var posts = await _db.EmployerPosts
                .Include(p => p.User).ThenInclude(u => u.EmployerProfile)
                .Include(p => p.Category)
                .Where(p =>
                p.Status == "Active" &&
                p.User.IsActive &&
                (
                    p.ExpiredAt == null ||                     // bài bình thường
                    p.ExpiredAt.Value.Date >= DateTime.Today   // bài thời vụ còn hạn
                ))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var p in posts)
                await AutoFixPostStatusAsync(p);

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

                SalaryMin = p.SalaryMin,
                SalaryMax = p.SalaryMax,
                SalaryType = p.SalaryType,
                SalaryDisplay = FormatSalary(p.SalaryMin, p.SalaryMax, p.SalaryType),

                Requirements = p.Requirements,
                WorkHours = p.WorkHours,

                ExpiredAtText = p.ExpiredAt?.ToString("dd/MM/yyyy"),


                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
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


        public async Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId, bool isAdmin = false, bool isOwner = false)
            {
            // 1️⃣ Lấy tất cả bài đăng của employer đó
            var posts = await _repo.GetByUserAsync(userId);

            foreach (var p in posts)
                await AutoFixPostStatusAsync(p);

            // 2️⃣ Lọc theo quyền truy cập
            if (isAdmin)
                {
                //  Admin xem được tất cả — kể cả Deleted, Blocked, Archived, Expired
                posts = posts
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
                }
            else if (isOwner)
                {
                //  Employer đang đăng nhập (chính chủ)
                // xem được tất cả bài trừ Deleted
                posts = posts
                    .Where(x => x.Status != "Deleted")
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
                }
            else
                {
                //  Seeker hoặc người ngoài chỉ thấy bài Active + còn hạn
                posts = posts
                    .Where(x =>
                        x.Status == "Active" &&
                        x.User.IsActive &&
                        (x.ExpiredAt == null || x.ExpiredAt.Value.Date >= DateTime.Today)
                    )
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
                }

            // 3️⃣ Lấy toàn bộ ảnh bài đăng (chỉ 1 query)
            var postIds = posts.Select(x => x.EmployerPostId).ToList();

            var imageLookup = await _db.Images
                .Where(i => i.EntityType == "EmployerPost" && postIds.Contains(i.EntityId))
                .GroupBy(i => i.EntityId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(i => i.Url).ToList()
                );

            // 4️⃣ Map ra DTO
            var result = posts.Select(p => new EmployerPostDtoOut
                {
                EmployerPostId = p.EmployerPostId,
                EmployerId = p.UserId,
                Title = p.Title,
                Description = p.Description,

                SalaryMin = p.SalaryMin,
                SalaryMax = p.SalaryMax,
                SalaryType = p.SalaryType,
                SalaryDisplay = FormatSalary(p.SalaryMin, p.SalaryMax, p.SalaryType),

                Requirements = p.Requirements,
                WorkHours = p.WorkHours,

                ExpiredAtText = p.ExpiredAt?.ToString("dd/MM/yyyy"),

                Location = p.Location,
                PhoneContact = p.PhoneContact,
                CategoryName = p.Category?.Name,
                EmployerName = p.User.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = p.CreatedAt,
                Status = p.Status,
                ImageUrls = imageLookup.ContainsKey(p.EmployerPostId)
                            ? imageLookup[p.EmployerPostId]
                            : new List<string>()
                }).ToList();

            return result;
            }

        public async Task<EmployerPostDtoOut?> GetByIdAsync(int id, int? requesterId = null, bool isAdmin = false)
        {
            var post = await _repo.GetByIdAsync(id);
            if (post == null)
                return null;

            await AutoFixPostStatusAsync(post);

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

            // REJECT nếu bài đã hết hạn (ngày < hôm nay)
            if (post.ExpiredAt != null && post.ExpiredAt.Value.Date < DateTime.Today)
                {
                if (!isAdmin && !isOwner)
                    return null; // hoặc throw "Bài đăng đã hết hạn."
                }

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

                SalaryMin = post.SalaryMin,
                SalaryMax = post.SalaryMax,
                SalaryType = post.SalaryType,
                SalaryDisplay = FormatSalary(post.SalaryMin, post.SalaryMax, post.SalaryType),

                Requirements = post.Requirements,
                WorkHours = post.WorkHours,

                ExpiredAtText = post.ExpiredAt?.ToString("dd/MM/yyyy"),

                Location = post.Location,
                ProvinceId = post.ProvinceId,
                DistrictId = post.DistrictId,
                WardId = post.WardId,
                PhoneContact = post.PhoneContact,
                CategoryName = post.Category?.Name,
                EmployerName = post.User.EmployerProfile?.DisplayName ?? "Nhà tuyển dụng",
                CreatedAt = post.CreatedAt,
                Status = post.Status,
                CompanyLogo = user.EmployerProfile?.AvatarUrl ?? "",
                ImageUrls = images
            };
            }

        public async Task<IEnumerable<EmployerPostDtoOut>> FilterAsync(string status, int? currentUserId, bool isAdmin)
            {
            status = status.ToLower();

            IQueryable<EmployerPost> query = _db.EmployerPosts
                .Include(x => x.User)
                .Include(x => x.Category);

            switch (status)
                {
                case "active":
                    query = query.Where(x =>
                        x.Status == "Active" &&
                        x.User.IsActive &&
                        (x.ExpiredAt == null || x.ExpiredAt.Value.Date >= DateTime.Today));
                    break;

                case "archived":
                    query = query.Where(x =>
                        x.Status == "Archived" &&
                        (isAdmin || x.UserId == currentUserId));
                    break;

                case "expired":
                    query = query.Where(x =>
                        x.ExpiredAt != null &&
                        x.ExpiredAt.Value.Date < DateTime.Today &&
                        (isAdmin || x.UserId == currentUserId));
                    break;

                case "blocked":
                    if (!isAdmin) return Enumerable.Empty<EmployerPostDtoOut>();
                    query = query.Where(x => x.Status == "Blocked");
                    break;

                case "deleted":
                    if (!isAdmin) return Enumerable.Empty<EmployerPostDtoOut>();
                    query = query.Where(x => x.Status == "Deleted");
                    break;

                default:
                    throw new Exception("Trạng thái không hợp lệ.");
                }

            var posts = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            foreach (var p in posts)
                await AutoFixPostStatusAsync(p);

            return posts.Select(x => new EmployerPostDtoOut
                {
                EmployerPostId = x.EmployerPostId,
                EmployerId = x.UserId,
                Title = x.Title,
                Description = x.Description,

                SalaryMin = x.SalaryMin,
                SalaryMax = x.SalaryMax,
                SalaryType = x.SalaryType,
                SalaryDisplay = FormatSalary(x.SalaryMin, x.SalaryMax, x.SalaryType),

                Requirements = x.Requirements,
                WorkHours = x.WorkHours,

                ExpiredAtText = x.ExpiredAt?.ToString("dd/MM/yyyy"),


                Location = x.Location,
                PhoneContact = x.PhoneContact,
                CategoryName = x.Category?.Name,
                EmployerName = x.User?.EmployerProfile?.DisplayName,
                CreatedAt = x.CreatedAt,
                Status = x.Status
                });
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

            post.SalaryMin = dto.SalaryMin;
            post.SalaryMax = dto.SalaryMax;
            post.SalaryType = dto.SalaryType;

            post.Requirements = dto.Requirements;
            post.Location = fullLocation;

            post.ProvinceId = dto.ProvinceId ?? post.ProvinceId;
            post.DistrictId = dto.DistrictId ?? post.DistrictId;
            post.WardId = dto.WardId ?? post.WardId;

            post.WorkHours = $"{dto.WorkHourStart} - {dto.WorkHourEnd}";
            post.CategoryId = dto.CategoryID;
            post.PhoneContact = dto.PhoneContact;
            post.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(dto.ExpiredAt))
                {
                post.ExpiredAt = ParseDate(dto.ExpiredAt);
                }


            await _repo.UpdateAsync(post);

            // --- Validate Salary ---
            if (post.SalaryMin == null && post.SalaryMax == null && post.SalaryType == null)
                {
                // Thỏa thuận → giữ null
                }
            else
                {
                if (post.SalaryMin < 0)
                    throw new Exception("SalaryMin không hợp lệ.");

                if (post.SalaryMax < post.SalaryMin)
                    throw new Exception("SalaryMax phải ≥ SalaryMin.");

                if (post.SalaryType == null)
                    throw new Exception("SalaryType là bắt buộc.");
                }


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

            string embedText =
                    $@"Tiêu đề: {post.Title}
                    Mô tả công việc: {post.Description}
                    Yêu cầu ứng viên: {post.Requirements}
                    Ngành nghề: {category?.Name}
                    Giờ làm việc: {post.WorkHours}";




            var (vector, _) = await EnsureEmbeddingAsync("EmployerPost", post.EmployerPostId, embedText);

            await _ai.UpsertVectorAsync(
                ns: "employer_posts",
                id: $"EmployerPost:{post.EmployerPostId}",
                vector: vector,
                metadata: new
                    {
                    numericPostId = post.EmployerPostId,
                    categoryId = post.CategoryId,
                    provinceId = post.ProvinceId,
                    districtId = post.DistrictId,
                    wardId = post.WardId,
                    title = post.Title ?? "",
                    status = post.Status
                    });

            await AutoFixPostStatusAsync(post);

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

            // Không chạy AI nếu bài đã hết hạn
            if (post.ExpiredAt != null && post.ExpiredAt.Value.Date < DateTime.Today)
                {
                return new EmployerPostResultDto
                    {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                    };
                }

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
                $@"Tiêu đề: {post.Title}
                Mô tả công việc: {post.Description}
                Yêu cầu ứng viên: {post.Requirements}
                Ngành nghề: {category?.Name}
                Giờ làm việc: {post.WorkHours}";



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
                numericPostId = post.EmployerPostId,
                categoryId = post.CategoryId,
                provinceId = post.ProvinceId,
                districtId = post.DistrictId,
                wardId = post.WardId,
                title = post.Title ?? "",
                status = post.Status
                });

            //  Chấm điểm & lọc ứng viên
            var scored = await ScoreAndFilterCandidatesAsync(vector, post);

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

        //  SCORING LOGIC (Category filter + Distance ≤100km + Hybrid score)
        private async Task<List<(JobSeekerPost Seeker, double Score, int? CvId)>>
        ScoreAndFilterCandidatesAsync(
            float[] employerVector,
            EmployerPost post)
            {
            // ---------------------------------------
            // 1) LẤY THÔNG TIN ĐỊA CHỈ EMPLOYER (NGUỒN)
            // ---------------------------------------
            int employerProvinceId = post.ProvinceId;
            int employerDistrictId = post.DistrictId;
            int employerWardId = post.WardId;
            string jobLocation = post.Location;
            int? mustMatchCategoryId = post.CategoryId;


            // ---------------------------------------
            // 2) LỌC THEO CATEGORY VỚI RULE "OTHER"
            // ---------------------------------------

            // Tìm category "Other" / "Khác" (tùy bạn đặt name)
            var otherCategoryId = await _db.Categories
                .Where(c => c.Name == "Other" || c.Name == "Khác")
                .Select(c => (int?)c.CategoryId)
                .FirstOrDefaultAsync();

            bool isOtherPost =
                otherCategoryId.HasValue &&
                mustMatchCategoryId.HasValue &&
                mustMatchCategoryId.Value == otherCategoryId.Value;

            IQueryable<JobSeekerPost> query = _db.JobSeekerPosts
                .Where(js => js.Status == "Active");

            if (otherCategoryId.HasValue)
                {
                if (isOtherPost)
                    {
                    // Bài tuyển chọn "Other" → chỉ match JS "Other"
                    query = query.Where(js => js.CategoryId == otherCategoryId.Value);
                    }
                else
                    {
                    // Bài tuyển KHÔNG phải "Other" → loại bỏ JS "Other"
                    query = query.Where(js => js.CategoryId != otherCategoryId.Value);
                    }
                }

            var categoryFiltered = await query
                .Include(js => js.User)
                .Include(js => js.Category)
                .ToListAsync();

            if (!categoryFiltered.Any())
                return new List<(JobSeekerPost, double, int?)>();

            // ---------------------------------------
            // 3) LỌC DISTANCE / ĐỊA CHỈ
            // ---------------------------------------
            var locationFiltered = new List<JobSeekerPost>();

            foreach (var js in categoryFiltered)
                {
                bool ok = await IsWithinDistanceAsync(
                    seekerProvince: js.ProvinceId,
                    seekerDistrict: js.DistrictId,
                    seekerWard: js.WardId,
                    jobProvince: employerProvinceId,
                    jobDistrict: employerDistrictId,
                    jobWard: employerWardId,
                    seekerLocation: js.PreferredLocation,
                    jobLocation: jobLocation
                );

                if (ok)
                    locationFiltered.Add(js);
                }

            if (!locationFiltered.Any())
                return new List<(JobSeekerPost, double, int?)>();

            // ---------------------------------------
            // 4) DANH SÁCH ID ĐÃ QUA HARD FILTER
            // ---------------------------------------
            var allowedIds = locationFiltered
                .Select(x => x.JobSeekerPostId)
                .ToHashSet();

            var pineconeIds = allowedIds.ToList();

            // ---------------------------------------
            // 5) QUERY PINECONE CHỈ TRÊN allowedIds
            // ---------------------------------------
            var pineconeMatches = await _ai.QueryWithIDsAsync(
                "job_seeker_posts",
                employerVector,
                pineconeIds,
                topK: pineconeIds.Count
            );

            if (!pineconeMatches.Any())
                return new List<(JobSeekerPost, double, int?)>();

            // ---------------------------------------
            // 6) GHÉP SCORE TRỞ LẠI
            // ---------------------------------------
            var results = new List<(JobSeekerPost, double, int?)>();

            foreach (var m in pineconeMatches)
                {
                if (!m.Id.StartsWith("JobSeekerPost:"))
                    continue;

                if (!int.TryParse(m.Id.Split(':')[1], out int seekerId))
                    continue;

                if (!allowedIds.Contains(seekerId))
                    continue;

                var seeker = locationFiltered
                    .FirstOrDefault(x => x.JobSeekerPostId == seekerId);

                if (seeker == null)
                    continue;

                results.Add((seeker, m.Score, seeker.SelectedCvId));
                }

            return results;
            }

        private async Task<bool> IsWithinDistanceAsync(
     int seekerProvince,
     int seekerDistrict,
     int seekerWard,
     int jobProvince,
     int jobDistrict,
     int jobWard,
     string seekerLocation,
     string jobLocation)
            {
            // 1) TRÙNG WARD → MATCH
            if (seekerWard == jobWard && seekerWard != 0)
                return true;

            // 2) TRÙNG DISTRICT → MATCH
            if (seekerDistrict == jobDistrict && seekerDistrict != 0)
                return true;

            // 3) TRÙNG PROVINCE → MATCH
            if (seekerProvince == jobProvince && seekerProvince != 0)
                return true;

            // 4) Nếu không trùng → tính khoảng cách <=300km
            try
                {
                var fromCoord = await _map.GetCoordinatesAsync(seekerLocation);
                var toCoord = await _map.GetCoordinatesAsync(jobLocation);

                if (fromCoord != null && toCoord != null)
                    {
                    double dist = _map.ComputeDistanceKm(
                        fromCoord.Value.lat, fromCoord.Value.lng,
                        toCoord.Value.lat, toCoord.Value.lng
                    );

                    return dist <= 300;
                    }
                }
            catch { }

            return false;
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
    int employerPostId, int take = 5, int skip = 0)
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

        private async Task<EmployerPostDtoOut> BuildCleanPostDto(EmployerPostModel post)
        {
            var category = await _db.Categories.FindAsync(post.CategoryId);
            var user = await _db.Users.FindAsync(post.UserId);
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

                SalaryMin = post.SalaryMin,
                SalaryMax = post.SalaryMax,
                SalaryType = post.SalaryType,
                SalaryDisplay = FormatSalary(post.SalaryMin, post.SalaryMax, post.SalaryType),


                Requirements = post.Requirements,
                WorkHours = post.WorkHours,

                WorkHourStart = post.WorkHours?.Split('-')[0].Trim(),
                WorkHourEnd = post.WorkHours?.Split('-').Length > 1
                ? post.WorkHours.Split('-')[1].Trim()
                : null,

                ExpiredAtText = post.ExpiredAt?.ToString("dd/MM/yyyy"),

                Location = post.Location,

                //  TRẢ ĐÚNG VỀ CLIENT
                //ProvinceId = post.ProvinceId,
                //DistrictId = post.DistrictId,
                //WardId = post.WardId,

                PhoneContact = post.PhoneContact,
                CategoryName = category?.Name,
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

        private static string FormatSalary(decimal? min, decimal? max, int? type)
            {
            if (min == null && max == null && type == null)
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

        private DateTime? ParseDate(string? input)
            {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string[] formats =
            {
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm",
        "dd-MM-yyyy",
        "dd-MM-yyyy HH:mm",
        "d/M/yyyy",
        "d/M/yyyy HH:mm",
    };

            if (DateTime.TryParseExact(
                input,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime parsed))
                {
                return parsed;
                }

            throw new Exception($"Ngày hết hạn không hợp lệ. Hãy nhập theo dạng dd/MM/yyyy (VD: 30/11/2025).");
            }


        private async Task<EmployerSubscription> EnsureFreeSubscriptionAsync(int userId)
            {
            var now = DateTime.Now;

            // Lấy gói Free
            var freePlan = await _db.EmployerPlans.FirstOrDefaultAsync(p => p.PlanName == "Free");
            if (freePlan == null)
                throw new Exception("Không tìm thấy gói Free trong hệ thống.");

            // Subscription free gần nhất
            var sub = await _db.EmployerSubscriptions
                .Where(s => s.UserId == userId && s.PlanId == freePlan.PlanId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            // 1️⃣ Chưa có free → tạo mới
            if (sub == null)
                {
                sub = new EmployerSubscription
                    {
                    UserId = userId,
                    PlanId = freePlan.PlanId,
                    RemainingPosts = freePlan.MaxPosts,
                    StartDate = now,
                    EndDate = now.AddDays(freePlan.DurationDays ?? 30),
                    Status = "Active",
                    CreatedAt = now,
                    UpdatedAt = now
                    };

                _db.EmployerSubscriptions.Add(sub);
                await _db.SaveChangesAsync();
                return sub;
                }

            // 2️⃣ ĐÃ CÓ FREE → CHỈ reset khi QUA THÁNG MỚI
            bool isDifferentMonth = sub.StartDate.Month != now.Month || sub.StartDate.Year != now.Year;

            if (isDifferentMonth)
                {
                sub.RemainingPosts = freePlan.MaxPosts;
                sub.StartDate = now;
                sub.EndDate = now.AddDays(freePlan.DurationDays ?? 30);
                sub.Status = "Active";
                sub.UpdatedAt = now;

                await _db.SaveChangesAsync();
                }

            return sub;
            }


        private async Task AutoFixPostStatusAsync(EmployerPost post)
            {
            var today = DateTime.Today;

            // Nếu expired
            if (post.ExpiredAt != null && post.ExpiredAt.Value.Date < today)
                {
                if (post.Status != "Expired")
                    {
                    post.Status = "Expired";
                    post.UpdatedAt = DateTime.Now;

                    // Xóa vector AI
                    await _ai.DeleteVectorAsync("employer_posts", $"EmployerPost:{post.EmployerPostId}");
                    await _repo.UpdateAsync(post);
                    await _repo.SaveChangesAsync();
                    }
                }
            else
                {
                // Nếu chưa hết hạn mà status đangExpired -> Auto activate lại
                if (post.Status == "Expired")
                    {
                    post.Status = "Active";
                    post.UpdatedAt = DateTime.Now;

                    // Recreate embedding + upsert
                    var category = await _db.Categories.FindAsync(post.CategoryId);

                    string embedText =
                        $@"Tiêu đề: {post.Title}
                Mô tả: {post.Description}
                Yêu cầu: {post.Requirements}
                Ngành: {category?.Name}
                Giờ làm việc: {post.WorkHours}";

                    var (vector, _) = await EnsureEmbeddingAsync("EmployerPost", post.EmployerPostId, embedText);

                    await _ai.UpsertVectorAsync("employer_posts", $"EmployerPost:{post.EmployerPostId}", vector,
                        new
                            {
                            numericPostId = post.EmployerPostId,
                            categoryId = post.CategoryId,
                            provinceId = post.ProvinceId,
                            districtId = post.DistrictId,
                            wardId = post.WardId,
                            title = post.Title ?? "",
                            status = post.Status
                            });

                    await _repo.UpdateAsync(post);
                    await _repo.SaveChangesAsync();
                    }
                }
            }
        }
    }