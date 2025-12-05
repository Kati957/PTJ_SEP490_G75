using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PTJ_Service.PaymentsService;

namespace PTJ_API.BackgroundServices
    {
    public class PayOsSyncService : BackgroundService
        {
        private readonly ILogger<PayOsSyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public PayOsSyncService(
            ILogger<PayOsSyncService> logger,
            IServiceScopeFactory scopeFactory)
            {
            _logger = logger;
            _scopeFactory = scopeFactory;
            }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
            _logger.LogInformation("🚀 PayOS Sync Service started...");

            while (!stoppingToken.IsCancellationRequested)
                {
                try
                    {
                    using var scope = _scopeFactory.CreateScope();

                    var paymentService =
                        scope.ServiceProvider.GetRequiredService<IEmployerPaymentService>();

                    int updated = await paymentService.SyncPendingTransactionsAsync();

                    if (updated > 0)
                        _logger.LogInformation($"🔄 Đồng bộ thành công {updated} giao dịch pending.");
                    }
                catch (Exception ex)
                    {
                    _logger.LogError(ex, "❌ Lỗi đồng bộ PayOS.");
                    }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
