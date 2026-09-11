using GemApi.Data;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace GemApi.Services;

public class PuneBidAlertService : IPuneBidAlertService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPuneBidEmailService _emailService;
    private readonly PuneBidAlertSettings _settings;
    private readonly ILogger<PuneBidAlertService> _logger;

    public PuneBidAlertService(
        ApplicationDbContext dbContext,
        IPuneBidEmailService emailService,
        IOptions<PuneBidAlertSettings> options,
        ILogger<PuneBidAlertService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task CheckForPuneBidsAsync(
        CancellationToken cancellationToken)
    {
        var state = await _dbContext.PuneBidAlertStates
            .SingleAsync(
                x => x.Id == 1,
                cancellationToken);

        var lastCheckedAt = state.LastCheckedAt;

        // Everything created/updated after this point
        // will be processed during the next check.
        var checkStartedAt = DateTime.Now;

        _logger.LogInformation(
            "Pune bid alert check started. LastCheckedAt: {LastCheckedAt}",
            lastCheckedAt);

        // Get only records that changed between the previous
        // successful checkpoint and the beginning of this check.
        var changedBids = await _dbContext.GeMbidExtracts
            .AsNoTracking()
            .Where(x =>
                (
                    x.CreatedOn > lastCheckedAt &&
                    x.CreatedOn <= checkStartedAt
                )
                ||
                (
                    x.UpdatedOn != null &&
                    x.UpdatedOn > lastCheckedAt &&
                    x.UpdatedOn <= checkStartedAt
                ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} new or updated bids.",
            changedBids.Count);

        // Search "Pune" across every public property.
        var puneBids = changedBids
            .Where(ContainsPune)
            .ToList();

        _logger.LogInformation(
            "Found {Count} new or updated Pune-related bids.",
            puneBids.Count);

        if (puneBids.Count == 0)
        {
            // Nothing needs to be emailed, so the checkpoint
            // can safely move forward.
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "No Pune bids found. LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Determine the timestamp representing this particular
        // bid version/change.
        var candidates = puneBids
            .Select(bid => new
            {
                Bid = bid,
                ChangeDetectedOn =
                    bid.UpdatedOn ?? bid.CreatedOn
            })
            .ToList();

        // Find which changes have already been successfully emailed.
        var bidIds = candidates
            .Select(x => x.Bid.Id)
            .Distinct()
            .ToList();

        var existingAlerts = await _dbContext.PuneBidAlertSents
            .AsNoTracking()
            .Where(x => bidIds.Contains(x.BidId))
            .ToListAsync(cancellationToken);

        var unsentCandidates = candidates
            .Where(candidate =>
                !existingAlerts.Any(sent =>
                    sent.BidId == candidate.Bid.Id &&
                    sent.ChangeDetectedOn ==
                        candidate.ChangeDetectedOn))
            .ToList();

        _logger.LogInformation(
            "{Count} Pune bid change(s) require email notification.",
            unsentCandidates.Count);

        if (unsentCandidates.Count == 0)
        {
            // Everything found in this interval has already been
            // successfully notified.
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "All Pune bid changes were already notified. " +
                "LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Convert database entities into the small DTO containing
        // only the fields we want in the email.
        var emailBids = unsentCandidates
            .Select(x => new PuneBidAlertDto
            {
                BidNumber = x.Bid.BidNumber ?? string.Empty,

                CardStartDate =
                    x.Bid.CardStartDate?.ToString(),

                CardEndDate =
                    x.Bid.CardEndDate?.ToString(),

                CategoryKey =
                    x.Bid.CategoryKey,

                OfficeName =
                    x.Bid.OfficeName,

                OrganisationName =
                    x.Bid.OrganisationName,

                ConsigneeName =
                    x.Bid.ConsigneeName
            })
            .ToList();

        // IMPORTANT:
        // If sending fails, this method throws and LastCheckedAt
        // is NOT updated. The next check will retry the email.
        await _emailService.SendPuneBidAlertAsync(
            emailBids,
            cancellationToken);

        // Email was successfully sent.
        // Now record every notified bid version.
        var sentOn = DateTime.Now;

        foreach (var candidate in unsentCandidates)
        {
            _dbContext.PuneBidAlertSents.Add(
                new PuneBidAlertSent
                {
                    BidId = candidate.Bid.Id,

                    BidNumber =
                        candidate.Bid.BidNumber ?? string.Empty,

                    BidUpdatedOn =
                        candidate.Bid.UpdatedOn,

                    ChangeDetectedOn =
                        candidate.ChangeDetectedOn,

                    SentOn = sentOn
                });
        }

        // Only move the checkpoint after the email and audit
        // records have been successfully prepared.
        state.LastCheckedAt = checkStartedAt;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Pune bid alert processing completed. " +
            "Sent: {SentCount}. LastCheckedAt: {LastCheckedAt}.",
            unsentCandidates.Count,
            checkStartedAt);
    }

    private static bool ContainsPune(
        GeMbidExtract bid)
    {
        var properties = typeof(GeMbidExtract)
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(bid);

            if (value == null)
                continue;

            if (value.ToString()!
                .Contains(
                    "Pune",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}