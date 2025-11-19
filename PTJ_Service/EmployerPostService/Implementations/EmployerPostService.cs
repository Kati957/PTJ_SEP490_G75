using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces;
using PTJ_Data.Repositories.Interfaces.EPost;
using PTJ_Models;
using PTJ_Models.DTO.PostDTO;
using PTJ_Models.Models;
using PTJ_Service.AiService;
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
        private readonly LocationDisplayService _locDisplay;


        public EmployerPostService(
            IEmployerPostRepository repo,
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

        public async Task<EmployerPostResultDto> CreateEmployerPostAsync(EmployerPostDto dto)
            {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Dữ liệu không hợp lệ.");

            if (dto.UserID <= 0)
                throw new Exception("Thiếu thông tin UserID khi tạo bài đăng tuyển dụng.");

            string fullLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
            );

            // GỘP ĐỊA CHỈ CHI TIẾT
            if (!string.IsNullOrWhiteSpace(dto.DetailAddress))
                {
                fullLocation = $"{dto.DetailAddress}, {fullLocation}";
                }

            // 🧱 Tạo bài đăng mới
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

                // ⭐ LOCATION ID — thêm vào DB
                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardId = dto.WardId,

                CategoryId = dto.CategoryID,
                PhoneContact = dto.PhoneContact,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Status = "Active"
                };


            // ✅ Lưu DB để lấy ID thật
            await _repo.AddAsync(post);
            await _db.SaveChangesAsync();

            // ✅ Load lại entity đầy đủ (có User và Category)
            var freshPost = await _db.EmployerPosts
                .Include(x => x.User)
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerPostId == post.EmployerPostId);

            if (freshPost == null)
                throw new Exception("Không thể load lại bài đăng vừa tạo.");

            // 🧠 Tạo embedding vector
            var (vector, hash) = await EnsureEmbeddingAsync(
                "EmployerPost",
                freshPost.EmployerPostId,
                $"{freshPost.Title}. {freshPost.Description}. Yêu cầu: {freshPost.Requirements}. Lương: {freshPost.Salary}"
            );

            // 📤 Upsert vector vào Pinecone
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

            // 🔍 Truy vấn ứng viên tương tự (top 100)
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

            // 🔢 Tính điểm và lọc ứng viên
            var scored = await ScoreAndFilterCandidatesAsync(
                        matches,
                        freshPost.CategoryId,
                        freshPost.Location ?? "",
                        freshPost.Title ?? "",
                        freshPost.Requirements ?? ""    // thêm tham số 5
                    );



            // 💾 Lưu gợi ý top 5
            var scoredWithCv = scored.Select(x => (x.Seeker, x.Score, x.CvId)).ToList();

            await UpsertSuggestionsAsync(
                "EmployerPost",
                freshPost.EmployerPostId,
                "JobSeekerPost",
                scoredWithCv,
                keepTop: 5
            );



            // 🧾 Lấy danh sách ứng viên đã được shortlist
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == freshPost.EmployerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            // 🎯 Chuẩn hóa danh sách ứng viên trả về
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
            SelectedCvId = x.CvId,   //  <<<<<<<<<<<<<<<<<<<<<<  🔥 THÊM DÒNG NÀY
            IsSaved = savedIds.Contains(x.Seeker.JobSeekerPostId)
            }
        })
    .ToList();


            // ✅ Trả kết quả cuối cùng
            return new EmployerPostResultDto
                {
                Post = await BuildCleanPostDto(freshPost),
                SuggestedCandidates = suggestions
                };
            }



        // READ

        public async Task<IEnumerable<EmployerPostDtoOut>> GetAllAsync()
            {
            var posts = await _repo.GetAllAsync();
            return posts.Select(p => new EmployerPostDtoOut
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
                EmployerName = p.User.Username,
                CreatedAt = p.CreatedAt,
                Status = p.Status
                });
            }

        public async Task<IEnumerable<EmployerPostDtoOut>> GetByUserAsync(int userId)
            {
            var posts = await _repo.GetByUserAsync(userId);
            return posts.Select(p => new EmployerPostDtoOut
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
            string fullLocation = await _locDisplay.BuildAddressAsync(
                dto.ProvinceId,
                dto.DistrictId,
                dto.WardId
            );

            if (!string.IsNullOrWhiteSpace(dto.DetailAddress))
                {
                fullLocation = $"{dto.DetailAddress}, {fullLocation}";
                }


            post.Title = dto.Title;
            post.Description = dto.Description;
            post.Salary = (!string.IsNullOrEmpty(dto.SalaryText) &&
               dto.SalaryText.ToLower().Contains("thoả thuận"))
                ? null
                : dto.Salary;
            post.Requirements = dto.Requirements;
            post.Location = fullLocation;
            post.ProvinceId = dto.ProvinceId;
            post.DistrictId = dto.DistrictId;
            post.WardId = dto.WardId;
            post.WorkHours = $"{dto.WorkHourStart} - {dto.WorkHourEnd}";
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

            // 🔄 Tạo embedding lại
            var (vector, _) = await EnsureEmbeddingAsync(
                "EmployerPost",
                post.EmployerPostId,
                $"{post.Title}. {post.Description}. Yêu cầu: {post.Requirements}. Địa điểm: {post.Location}. Lương: {post.Salary}"
            );

            // 🔄 Upsert vector vào Pinecone
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

            // 🔍 Query ứng viên tương tự
            var matches = await _ai.QuerySimilarAsync("job_seeker_posts", vector, 100);

            if (!matches.Any())
                {
                return new EmployerPostResultDto
                    {
                    Post = await BuildCleanPostDto(post),
                    SuggestedCandidates = new List<AIResultDto>()
                    };
                }

            // 🧠 Chấm điểm và lọc ứng viên
            var scored = await ScoreAndFilterCandidatesAsync(
                matches,
                post.CategoryId,
                post.Location ?? "",
                post.Title ?? "",
                post.Requirements ?? ""
            );

            // 🆕 🆕 🆕---------------------------------------------
            // 📌 LƯU LẠI GỢI Ý TOP 5 TRONG DB (FIX CHÍNH)
            await UpsertSuggestionsAsync(
                "EmployerPost",
                post.EmployerPostId,
                "JobSeekerPost",
                scored,
                keepTop: 5
            );
            // 🆕 🆕 🆕---------------------------------------------

            // Danh sách ID đã save
            var savedIds = await _db.EmployerShortlistedCandidates
                .Where(x => x.EmployerPostId == employerPostId)
                .Select(x => x.JobSeekerId)
                .ToListAsync();

            // Build danh sách trả về
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
            SelectedCvId = x.CvId,   // <<<<<<<<<<<<<< THÊM CVID
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

        // ================================================
        // ⚙️ SCORING LOGIC (Category filter + Distance ≤100km + Hybrid score)
        // ================================================
        private async Task<List<(JobSeekerPost Seeker, double Score, int? CvId)>>
ScoreAndFilterCandidatesAsync(
    List<(string Id, double Score)> matches,
    int? mustMatchCategoryId,
    string employerLocation,
    string employerTitle,
    string employerRequirements)
            {
            var result = new List<(JobSeekerPost, double, int?)>();

            float[] jdEmbedding = await _ai.CreateEmbeddingAsync(
                $"{employerTitle}. {employerRequirements}. {employerLocation}"
            );

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

                // ❗ CATEGORY FILTER
                if (mustMatchCategoryId.HasValue &&
                    seeker.CategoryId != mustMatchCategoryId.Value)
                    continue;

                // ❗ LẤY CV CỦA BÀI ĐĂNG (ĐÚNG YÊU CẦU)
                JobSeekerCv? cv = null;
                if (seeker.SelectedCvId.HasValue)
                    {
                    cv = await _db.JobSeekerCvs
                        .FirstOrDefaultAsync(c =>
                            c.Cvid == seeker.SelectedCvId.Value &&
                            c.JobSeekerId == seeker.UserId);
                    }

                int? cvId = seeker.SelectedCvId;

                // === CV EMBEDDING ===
                float[] cvEmbedding = Array.Empty<float>();

                if (cv != null)
                    {
                    var cvEmbeddingRecord = await _db.AiEmbeddingStatuses
                        .FirstOrDefaultAsync(e =>
                            e.EntityType == "JobSeekerCV" &&
                            e.EntityId == cv.Cvid);

                    if (cvEmbeddingRecord != null && cvEmbeddingRecord.VectorData != null)
                        cvEmbedding = JsonConvert.DeserializeObject<float[]>(cvEmbeddingRecord.VectorData);
                    }

                double cvCosine = 0;
                if (cvEmbedding.Length == jdEmbedding.Length)
                    cvCosine = Cosine(cvEmbedding, jdEmbedding);

                double cvBoost = cvCosine * 0.35;

                // === SKILL BOOST (nếu bài đăng có CV) ===
                double skillBoost = 0;

                if (cv != null)
                    {
                    var cvSkills = (cv.Skills ?? "")
                        .ToLower()
                        .Split(',', ';', '.', ' ')
                        .Where(x => x.Length > 1)
                        .Distinct()
                        .ToList();

                    var jdReq = (employerRequirements ?? "")
                        .ToLower()
                        .Split(',', ';', '.', ' ')
                        .Where(x => x.Length > 1)
                        .Distinct()
                        .ToList();

                    int matched = cvSkills.Count(x => jdReq.Contains(x));

                    if (cvSkills.Count > 0)
                        {
                        double overlap = (double)matched / cvSkills.Count;
                        skillBoost = overlap * 0.25;
                        }
                    }

                // === LOCATION SCORING ===
                double baseScore = await ComputeHybridScoreAsync(
                    m.Score,
                    employerLocation,
                    seeker.PreferredLocation
                );

                // === TỔNG ĐIỂM ===
                double finalScore = Math.Clamp(baseScore + cvBoost + skillBoost, 0, 1);

                result.Add((seeker, finalScore, cvId));
                }

            return result;
            }




        private double Cosine(float[] a, float[] b)
            {
            double dot = 0, da = 0, db = 0;

            for (int i = 0; i < a.Length; i++)
                {
                dot += a[i] * b[i];
                da += a[i] * a[i];
                db += b[i] * b[i];
                }

            return dot / (Math.Sqrt(da) * Math.Sqrt(db) + 1e-9);
            }



        private async Task<double> ComputeHybridScoreAsync(
            double contentMatchScore,
            string employerLocation,
            string? seekerLocation)
            {
            // === Trọng số ảnh hưởng ===
            const double WEIGHT_CONTENT_MATCH = 0.7;      // mức độ phù hợp nội dung
            const double WEIGHT_DISTANCE_FACTOR = 0.3;    // mức độ gần về vị trí ko so sanh vtr

            double locationMatchScore = 0.5; // trung lập nếu không xác định được

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
                            locationMatchScore = 0.0;
                        }
                    }
                }
            catch
                {
                locationMatchScore = 0.5; // fallback nếu API lỗi
                }

            // === Tổng điểm hybrid ===
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
                join jsp in _db.JobSeekerPosts.Include(x => x.User)
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

            // ✔ Xử lý SelectedCvId thủ công sau khi đã có dữ liệu từ SQL
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

        // ✔ Hỗ trợ tách CV=xxx
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

            return new EmployerPostDtoOut
                {
                EmployerPostId = post.EmployerPostId,
                EmployerId = post.UserId,

                Title = post.Title,
                Description = post.Description,
                Salary = post.Salary,
                SalaryText = post.Salary == null ? "Thỏa thuận" : $"{post.Salary}",
                Requirements = post.Requirements,
                WorkHours = post.WorkHours,
                
                WorkHourStart = post.WorkHours?.Split('-')[0].Trim(),
                WorkHourEnd = post.WorkHours?.Split('-').Length > 1
                ? post.WorkHours.Split('-')[1].Trim()
                : null,


                Location = post.Location,

                // ⭐ TRẢ ĐÚNG VỀ CLIENT
                //ProvinceId = post.ProvinceId,
                //DistrictId = post.DistrictId,
                //WardId = post.WardId,

                PhoneContact = post.PhoneContact,
                CategoryName = category?.Name,
                EmployerName = user?.Username ?? "",
                CreatedAt = post.CreatedAt,
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

        }
    }
