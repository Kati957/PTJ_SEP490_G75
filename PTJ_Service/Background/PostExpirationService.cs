using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Service.AiService;

public class PostExpirationService : BackgroundService
    {
    private readonly IServiceScopeFactory _scopeFactory;

    public PostExpirationService(IServiceScopeFactory scopeFactory)
        {
        _scopeFactory = scopeFactory;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
        while (!stoppingToken.IsCancellationRequested)
            {
            try
                {
                // Chờ đến 00:00 của ngày tiếp theo
                TimeSpan delay = DateTime.Today.AddDays(1) - DateTime.Now;
                await Task.Delay(delay, stoppingToken);

                await CleanupExpiredPosts(stoppingToken);
                }
            catch
                {
                // Nếu lỗi, chờ 1 phút chạy lại
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

    private async Task CleanupExpiredPosts(CancellationToken token)
        {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<JobMatchingOpenAiDbContext>();
        var ai = scope.ServiceProvider.GetRequiredService<IAIService>();

        DateTime today = DateTime.Today;

        // Lấy các bài đã hết hạn nhưng chưa có trạng thái Expired
        var expiredPosts = await db.EmployerPosts
            .Where(p =>
                p.ExpiredAt != null &&
                p.ExpiredAt.Value.Date < today &&
                p.Status != "Expired" &&
                p.Status != "Deleted"
            )
            .ToListAsync(token);

        foreach (var post in expiredPosts)
            {
            post.Status = "Expired";
            post.UpdatedAt = DateTime.Now;

            await ai.DeleteVectorAsync(
                "employer_posts",
                $"EmployerPost:{post.EmployerPostId}"
            );
            }

        await db.SaveChangesAsync(token);
        }
    }
