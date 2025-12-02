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
                // Tính thời điểm chạy tiếp theo: 00:00
                var now = DateTime.Now;
                var midnight = DateTime.Today.AddDays(1);
                var delay = midnight - now;

                await Task.Delay(delay, stoppingToken);

                await ProcessExpiredPosts(stoppingToken);
                }
            catch (Exception ex)
                {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

    private async Task ProcessExpiredPosts(CancellationToken token)
        {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobMatchingDbContext>();
        var ai = scope.ServiceProvider.GetRequiredService<IAIService>();

        var today = DateTime.Today;

        var expired = await db.EmployerPosts
            .Where(p => p.Status == "Active" &&
                        p.ExpiredAt != null &&
                        p.ExpiredAt.Value.Date < today)
            .ToListAsync(token);

        foreach (var post in expired)
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
