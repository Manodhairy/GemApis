using GemApi.Data;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GemApi.BackgroundServices
{
    public class BidEmailBackgroundService
        : BackgroundService
    {
        // Minimum records required before sending mail
        private const int MinimumRecordCount = 5;

        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger
            <BidEmailBackgroundService> _logger;

        public BidEmailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BidEmailBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken
                .IsCancellationRequested)
            {
                try
                {
                    await CheckNewRecordsAsync(
                        stoppingToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred while checking bids."
                    );
                }

                // Check the database every 1 minute
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken
                );
            }
        }

        private async Task CheckNewRecordsAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<
                        ApplicationDbContext>();

            // Existing GeMBidService reference
            var bidService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IGeMBidService>();

            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<
                        IEmailService>();

            int currentMaximumId =
                await context.GeMbidExtracts
                    .MaxAsync(
                        x => (int?)x.Id,
                        cancellationToken
                    ) ?? 0;

            var state =
                await context.BidNotificationStates
                    .FirstOrDefaultAsync(
                        x => x.Id == 1,
                        cancellationToken
                    );

            // First application run
            if (state == null)
            {
                state = new BidNotificationState
                {
                    Id = 1,
                    LastProcessedBidId =
                        currentMaximumId,
                    LastCheckedAt =
                        DateTime.UtcNow
                };

                await context.BidNotificationStates
                    .AddAsync(
                        state,
                        cancellationToken
                    );

                await context.SaveChangesAsync(
                    cancellationToken
                );

                _logger.LogInformation(
                    "Initial Bid Id saved: {Id}",
                    currentMaximumId
                );

                return;
            }

            if (currentMaximumId <=
                state.LastProcessedBidId)
            {
                return;
            }

            // Service calculates the actual counts
            var summary =
                await bidService
                    .GetNotificationSummaryAsync(
                        state.LastProcessedBidId,
                        currentMaximumId
                    );

            _logger.LogInformation(
                "Pending new records: {Count}",
                summary.NewRecordCount
            );

            // If fewer than MinimumRecordCount records, don't send mail
            // and don't update LastProcessedBidId either.
            // The count will accumulate with the next batch.
            if (summary.NewRecordCount <
                MinimumRecordCount)
            {
                _logger.LogInformation(
                    "Email not sent. Minimum {Minimum} records required.",
                    MinimumRecordCount
                );

                return;
            }

            // Send mail once threshold is reached
            await emailService
                .SendBidNotificationAsync(
                    summary,
                    MinimumRecordCount
                );

            // Only update state once mail succeeds
            state.LastProcessedBidId =
                currentMaximumId;

            state.LastCheckedAt =
                DateTime.UtcNow;

            await context.SaveChangesAsync(
                cancellationToken
            );

            _logger.LogInformation(
                "{Count} new bids found. Email sent.",
                summary.NewRecordCount
            );
        }
    }
}