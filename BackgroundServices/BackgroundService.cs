using GemApi.Data;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace GemApi.BackgroundServices
{
    public class BidEmailBackgroundService : BackgroundService
    {
        #region Fields

        // Minimum number of new records required
        // before sending an email.
        private const int MinimumRecordCount = 5;

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<BidEmailBackgroundService> _logger;

        #endregion


        #region Constructor

        public BidEmailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BidEmailBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #endregion


        #region ExecuteAsync

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Bid Email Background Service started."
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckNewRecordsAsync(
                        stoppingToken
                    );
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Bid Email Background Service is stopping."
                    );

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred while checking bids."
                    );
                }

                try
                {
                    // Check database every 1 minute
                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken
                    );
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Bid Email Background Service stopped."
            );
        }

        #endregion


        #region CheckNewRecordsAsync

        private async Task CheckNewRecordsAsync(
            CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();


            // ==========================================
            // DATABASE
            // ==========================================

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();


            // ==========================================
            // GEM BID SERVICE
            // ==========================================

            var bidService =
                scope.ServiceProvider
                    .GetRequiredService<IGeMBidService>();


            // ==========================================
            // EMAIL SERVICE
            // ==========================================

            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<IEmailService>();


            // ==========================================
            // CURRENT MAXIMUM BID ID
            // ==========================================

            int currentMaximumId =
                await context.GeMbidExtracts
                    .MaxAsync(
                        x => (int?)x.Id,
                        cancellationToken
                    ) ?? 0;


            _logger.LogInformation(
                "Current maximum GeM Bid ID: {Id}",
                currentMaximumId
            );


            // ==========================================
            // NOTIFICATION STATE
            // ==========================================

            var state =
                await context.BidNotificationStates
                    .FirstOrDefaultAsync(
                        x => x.Id == 1,
                        cancellationToken
                    );


            // ==========================================
            // FIRST APPLICATION RUN
            // ==========================================

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


            // ==========================================
            // NO NEW RECORDS
            // ==========================================

            if (
                currentMaximumId <=
                state.LastProcessedBidId)
            {
                _logger.LogInformation(
                    "No new GeM bids found."
                );

                return;
            }


            // ==========================================
            // GET NEW BID SUMMARY
            // ==========================================

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


            // ==========================================
            // LESS THAN 5 RECORDS
            // ==========================================

            if (
                summary.NewRecordCount <
                MinimumRecordCount)
            {
                _logger.LogInformation(
                    "Email not sent. " +
                    "Minimum {Minimum} records required. " +
                    "Current count: {Count}",
                    MinimumRecordCount,
                    summary.NewRecordCount
                );

                // IMPORTANT:
                // LastProcessedBidId is NOT updated.
                //
                // Therefore these records remain pending
                // for the next check.

                return;
            }


            // ==========================================
            // SEND EMAIL
            // ==========================================

            _logger.LogInformation(
                "Sending GeM bid notification email..."
            );


            await emailService
                .SendBidNotificationAsync(
                    summary,
                    MinimumRecordCount
                );


            // ==========================================
            // EMAIL SUCCESS
            // ==========================================

            state.LastProcessedBidId =
                currentMaximumId;

            state.LastCheckedAt =
                DateTime.UtcNow;


            await context.SaveChangesAsync(
                cancellationToken
            );


            _logger.LogInformation(
                "{Count} new bids found. " +
                "Email sent successfully. " +
                "LastProcessedBidId updated to {Id}.",
                summary.NewRecordCount,
                currentMaximumId
            );
        }

        #endregion
    }
}