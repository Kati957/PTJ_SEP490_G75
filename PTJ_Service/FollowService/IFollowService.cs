using PTJ_Models.DTO;

public interface IFollowService
{
    Task<bool> FollowEmployerAsync(int jobSeekerId, int employerId);
    Task<bool> UnfollowEmployerAsync(int jobSeekerId, int employerId);
    Task<bool> IsFollowingAsync(int jobSeekerId, int employerId);
    Task<IEnumerable<EmployerFollowDto>> GetFollowingListAsync(int jobSeekerId);
}
