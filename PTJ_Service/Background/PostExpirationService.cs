using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PTJ_Data;
using PTJ_Models.Models;
using PTJ_Service.AiService;

public class PostExpirationService : BackgroundService
    {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAIService _ai;

    public PostExpirationService(IServiceScopeFactory scopeFactory, IAIService ai)
        {
        _scopeFactory = scopeFactory;
        _ai = ai;
        }

    protected override async Task ExecuteAsync(CancellationToken token)
        {
        while (!token.IsCancellationRequested)
            {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobMatchingDbContext>();

            var today = DateTime.Today;

            var expiredPosts = await db.EmployerPosts
                .Where(p =>
                    p.Status == "Active" &&
                    p.ExpiredAt != null &&
                    p.ExpiredAt.Value.Date < today)
                .ToListAsync(token);

            foreach (var post in expiredPosts)
                {
                post.Status = "Expired";
                post.UpdatedAt = DateTime.Now;

                await _ai.DeleteVectorAsync(
                    "employer_posts",
                    $"EmployerPost:{post.EmployerPostId}"
                );
                }

            await db.SaveChangesAsync();

            // chạy mỗi ngày lúc 00:10
            var nextRun = DateTime.Today.AddDays(1).AddMinutes(10);
            await Task.Delay(nextRun - DateTime.Now, token);
            }
        }
    }
