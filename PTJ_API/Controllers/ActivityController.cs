using Microsoft.AspNetCore.Mvc;
using PTJ_Models.Models;
using PTJ_Service.UserActivityService;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly IUserActivityService _activityService;

    public ActivityController(IUserActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var logs = await _activityService.GetHistoryAsync(userId, from, to);

        var grouped = logs
            .GroupBy(x => x.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("dd/MM/yyyy"),
                Activities = g.Select(a => new
                {
                    Time = a.Timestamp.ToString("HH:mm"),
                    a.ActivityType,
                    a.Details
                })
            });

        return Ok(grouped);
    }
}
