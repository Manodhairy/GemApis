using GemApi.Data;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GemApi.BackgroundServices
{
    public class BidEmailBackgroundService
        : BackgroundService
    {
        // Minimum 100 नवीन records झाल्यावरच mail
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

                // प्रत्येक 1 मिनिटाने database check
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

            // Existing GeMBidService चा reference
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

            // Service actual counts calculate करेल
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

            // 100 पेक्षा कमी records असतील तर mail नाही
            // LastProcessedBidId सुद्धा update करायचा नाही.
            // पुढच्या records सोबत count accumulate होईल.
            if (summary.NewRecordCount <
                MinimumRecordCount)
            {
                _logger.LogInformation(
                    "Email not sent. Minimum {Minimum} records required.",
                    MinimumRecordCount
                );

                return;
            }

            // 100 किंवा जास्त झाल्यानंतर mail
            await emailService
                .SendBidNotificationAsync(
                    summary,
                    MinimumRecordCount
                );

            // Mail successful झाल्यावरच state update
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