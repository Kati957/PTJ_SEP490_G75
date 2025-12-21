using Microsoft.EntityFrameworkCore;
using PTJ_Data;
using PTJ_Data.Repositories.Interfaces.Ratings;
using PTJ_Models.DTO.RatingDto;
using PTJ_Models.Models;
using PTJ_Service.RatingService.Interfaces;

namespace PTJ_Service.RatingService.Implementations
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;
        private readonly JobMatchingOpenAiDbContext _context;

        public RatingService(IRatingRepository ratingRepository, JobMatchingOpenAiDbContext context)
        {
            _ratingRepository = ratingRepository;
            _context = context;
        }

        public async Task<bool> CreateRatingAsync(RatingCreateDto dto, int raterId)
        {
            //  1. Kiểm tra giá trị hợp lệ
            if (dto.RatingValue < 0 || dto.RatingValue > 5)
                throw new ArgumentException("Giá trị đánh giá phải từ 0 đến 5.");

            //  2. Tìm submission
            var submission = await _context.JobSeekerSubmissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubmissionId == dto.SubmissionId);

            if (submission == null)
                throw new Exception("Không tìm thấy hồ sơ ứng tuyển.");

            //  3. Tìm employerId qua EmployerPost
            var employerId = await _context.EmployerPosts
                .Where(p => p.EmployerPostId == submission.EmployerPostId)
                .Select(p => p.UserId)
                .FirstOrDefaultAsync();

            if (employerId == 0)
                throw new Exception("Không tìm thấy nhà tuyển dụng của bài đăng.");

            //  4. Kiểm tra quyền hợp lệ
            bool isValidRelation =
                (raterId == submission.JobSeekerId && dto.RateeId == employerId) ||
                (raterId == employerId && dto.RateeId == submission.JobSeekerId);

            if (!isValidRelation)
                throw new Exception($"Bạn không có quyền đánh giá người này. (raterId={raterId}, jobSeeker={submission.JobSeekerId}, employer={employerId})");

            //  5. Chỉ cho phép khi công việc đã hoàn tất
            if (submission.Status != "Completed" && submission.Status != "Accepted")
                throw new Exception("Chỉ có thể đánh giá sau khi công việc hoàn tất.");

            //  6. Kiểm tra đã tồn tại chưa
            bool exists = await _context.Ratings.AnyAsync(r =>
                r.RaterId == raterId &&
                r.RateeId == dto.RateeId &&
                r.SubmissionId == dto.SubmissionId);

            if (exists)
                throw new Exception("Bạn đã đánh giá người này rồi.");

            //  7. Tạo mới rating
            var rating = new Rating
            {
                RaterId = raterId,
                RateeId = dto.RateeId,
                SubmissionId = dto.SubmissionId,
                RatingValue = dto.RatingValue,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            //  8. Lưu DB
            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            return true;
        }

        //  Lấy danh sách rating của 1 người
        public async Task<IEnumerable<RatingViewDto>> GetRatingsForUserAsync(int rateeId)
        {
            var ratings = await _ratingRepository.GetRatingsByRateeAsync(rateeId);

            return ratings.Select(r => new RatingViewDto
            {
                RatingId = r.RatingId,
                RaterId = r.RaterId,
                RaterName = r.Rater?.Username,
                RatingValue = r.RatingValue ?? 0, //  Fix nullable
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });
        }

        //  Lấy điểm trung bình của 1 người
        public async Task<decimal> GetAverageRatingAsync(int rateeId)
        {
            return await _ratingRepository.GetAverageRatingByRateeAsync(rateeId);
        }
    }
}
