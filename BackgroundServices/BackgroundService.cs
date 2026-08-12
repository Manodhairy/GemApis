using GemApi.Data;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;
using GemApi.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GemApi.BackgroundServices
{
    public class BidEmailBackgroundService : BackgroundService
    {
        #region Fields

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<BidEmailBackgroundService> _logger;

        private readonly EmailScheduleSettings _scheduleSettings;

        private readonly TimeZoneInfo _indiaTimeZone;

        // Prevent duplicate email during the same scheduled minute
        private DateTime? _lastSentTime;

        #endregion


        #region Constructor

        public BidEmailBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BidEmailBackgroundService> logger,
            IOptions<EmailScheduleSettings> scheduleOptions)
        {
            _scopeFactory = scopeFactory;

            _logger = logger;

            _scheduleSettings = scheduleOptions.Value;

            try
            {
                // Linux / Docker / Windows with timezone database
                _indiaTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Asia/Kolkata"
                    );
            }
            catch
            {
                // Windows fallback
                _indiaTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "India Standard Time"
                    );
            }
        }

        #endregion


        #region ExecuteAsync

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Bid Email Background Service started."
            );

            _logger.LogInformation(
                "Configured email times: {Times}",
                string.Join(
                    ", ",
                    _scheduleSettings.Times
                )
            );


            while (
                !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ==========================================
                    // INDIA CURRENT TIME
                    // ==========================================

                    DateTime indiaTime =
                        TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.UtcNow,
                            _indiaTimeZone
                        );


                    _logger.LogDebug(
                        "Current India Time: {Time}",
                        indiaTime
                    );


                    // ==========================================
                    // CHECK SCHEDULE
                    // ==========================================

                    bool isScheduledTime =
                        IsScheduledTime(indiaTime);


                    if (isScheduledTime)
                    {
                        // ==========================================
                        // PREVENT DUPLICATE EMAIL
                        // ==========================================

                        bool alreadySent =
                            _lastSentTime.HasValue &&
                            _lastSentTime.Value.Date ==
                                indiaTime.Date &&
                            _lastSentTime.Value.Hour ==
                                indiaTime.Hour &&
                            _lastSentTime.Value.Minute ==
                                indiaTime.Minute;


                        if (!alreadySent)
                        {
                            _logger.LogInformation(
                                "Scheduled email time reached: {Time}",
                                indiaTime
                            );


                            // ==========================================
                            // SEND EMAIL
                            // ==========================================

                            await SendScheduledEmailAsync(
                                stoppingToken
                            );


                            // Mark as sent ONLY after successful email
                            _lastSentTime =
                                indiaTime;


                            _logger.LogInformation(
                                "Scheduled email completed at {Time}",
                                indiaTime
                            );
                        }
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred in Bid Email Background Service."
                    );
                }


                // ==========================================
                // CHECK EVERY 30 SECONDS
                // ==========================================

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        stoppingToken
                    );
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }


            _logger.LogInformation(
                "Bid Email Background Service stopped."
            );
        }

        #endregion


        #region IsScheduledTime

        private bool IsScheduledTime(
            DateTime indiaTime)
        {
            // Current time in HH:mm format
            string currentTime =
                indiaTime.ToString("HH:mm");


            // ==========================================
            // CHECK APPSETTINGS TIMES
            // ==========================================

            if (
                _scheduleSettings.Times == null ||
                _scheduleSettings.Times.Count == 0)
            {
                _logger.LogWarning(
                    "No EmailSchedule:Times configured."
                );

                return false;
            }


            return _scheduleSettings.Times.Any(
                configuredTime =>
                    string.Equals(
                        configuredTime.Trim(),
                        currentTime,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        #endregion


        #region SendScheduledEmailAsync

        private async Task SendScheduledEmailAsync(
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
            // BID SERVICE
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
                "Current maximum Bid ID: {Id}",
                currentMaximumId
            );


            // ==========================================
            // GET NOTIFICATION STATE
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
                state =
                    new BidNotificationState
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
                    "Initial Bid ID saved: {Id}",
                    currentMaximumId
                );
            }


            // ==========================================
            // PREVIOUS PROCESSED ID
            // ==========================================

            int lastProcessedId =
                state.LastProcessedBidId;


            _logger.LogInformation(
                "Last processed Bid ID: {Id}",
                lastProcessedId
            );


            // ==========================================
            // GET NEW BID SUMMARY
            // ==========================================

            var summary =
                await bidService
                    .GetNotificationSummaryAsync(
                        lastProcessedId,
                        currentMaximumId
                    );


            _logger.LogInformation(
                "New records found: {Count}",
                summary.NewRecordCount
            );


            // ==========================================
            // ALWAYS SEND EMAIL
            //
            // 0 bids  -> send email
            // 5 bids  -> send email
            // 60 bids -> send email
            // ==========================================

            await emailService
                .SendBidNotificationAsync(
                    summary,
                    0
                );


            // ==========================================
            // UPDATE STATE
            //
            // Only update after email succeeds.
            // ==========================================

            state.LastProcessedBidId =
                currentMaximumId;

            state.LastCheckedAt =
                DateTime.UtcNow;


            await context.SaveChangesAsync(
                cancellationToken
            );


            _logger.LogInformation(
                "Scheduled GeM email sent successfully. " +
                "New records: {Count}. " +
                "LastProcessedBidId updated to: {Id}",
                summary.NewRecordCount,
                currentMaximumId
            );
        }

        #endregion
    }
}