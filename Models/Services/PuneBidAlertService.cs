using GemApi.Data;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        // Use one consistent timestamp for this processing cycle.
        var checkStartedAt = DateTime.Now;

        _logger.LogInformation(
            "Location bid alert check started. LastCheckedAt: {LastCheckedAt}",
            lastCheckedAt);

        // Find records created or updated since the previous checkpoint.
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
            "Found {Count} new or updated bids before eligibility filtering.",
            changedBids.Count);

        // Only active IT bids from the three configured locations
        // are eligible for email notification.
        //
        // Active means:
        // CardStartDate <= checkStartedAt
        // CardEndDate   >= checkStartedAt
        //
        // Bids with missing start/end dates are excluded.
        var matchingBids = changedBids
            .Where(bid =>
                string.Equals(
                    bid.CategoryKey,
                    "IT",
                    StringComparison.OrdinalIgnoreCase))
            .Where(bid =>
                bid.CardStartDate.HasValue &&
                bid.CardEndDate.HasValue &&
                bid.CardStartDate.Value <= checkStartedAt &&
                bid.CardEndDate.Value >= checkStartedAt)
            .Select(bid => new
            {
                Bid = bid,
                Location = GetMatchingLocation(bid)
            })
            .Where(x => x.Location != null)
            .ToList();

        var puneBids = matchingBids
            .Where(x => x.Location == "Pune")
            .Select(x => x.Bid)
            .ToList();

        var nilgirisBids = matchingBids
            .Where(x => x.Location == "Nilgiris")
            .Select(x => x.Bid)
            .ToList();

        var lehBids = matchingBids
            .Where(x => x.Location == "Leh")
            .Select(x => x.Bid)
            .ToList();

        _logger.LogInformation(
            "Found {PuneCount} active IT Pune bid(s), " +
            "{NilgirisCount} active IT Nilgiris bid(s), " +
            "and {LehCount} active IT Leh bid(s).",
            puneBids.Count,
            nilgirisBids.Count,
            lehBids.Count);

        // No eligible active IT bids were found.
        // Advance the checkpoint so these changes are not repeatedly scanned.
        if (matchingBids.Count == 0)
        {
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "No active IT Pune/Nilgiris/Leh bids found. " +
                "LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Use the timestamp associated with this bid version.
        var candidates = matchingBids
            .Select(x => new
            {
                Bid = x.Bid,
                Location = x.Location!,
                ChangeDetectedOn =
                    x.Bid.UpdatedOn ?? x.Bid.CreatedOn
            })
            .ToList();

        var bidIds = candidates
            .Select(x => x.Bid.Id)
            .Distinct()
            .ToList();

        // Load existing notification history for these bid IDs.
        var existingAlerts = await _dbContext.PuneBidAlertSents
            .AsNoTracking()
            .Where(x => bidIds.Contains(x.BidId))
            .ToListAsync(cancellationToken);

        // Send only versions that have not already been notified.
        var unsentCandidates = candidates
            .Where(candidate =>
                !existingAlerts.Any(sent =>
                    sent.BidId == candidate.Bid.Id &&
                    sent.ChangeDetectedOn ==
                        candidate.ChangeDetectedOn))
            .ToList();

        _logger.LogInformation(
            "{Count} active IT Pune/Nilgiris/Leh bid change(s) " +
            "require email notification.",
            unsentCandidates.Count);

        if (unsentCandidates.Count == 0)
        {
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "All eligible active IT Pune/Nilgiris/Leh bid changes " +
                "were already notified. LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Build the email DTO using only the fields required by the template.
        var emailBids = unsentCandidates
            .Select(x => new PuneBidAlertDto
            {
                BidNumber =
                    x.Bid.BidNumber ?? string.Empty,

                PdfUrl =
                    x.Bid.PdfUrl,

                Location =
                    x.Location,

                CardStartDate =
                    x.Bid.CardStartDate?.ToString(),

                CardEndDate =
                    x.Bid.CardEndDate?.ToString(),

                CategoryKey =
                    x.Bid.CategoryKey,

                ItemCategory =
                    x.Bid.ItemCategory
            })
            .ToList();

        // If email sending fails, an exception is propagated.
        // The checkpoint is not advanced, allowing a subsequent retry.
        await _emailService.SendPuneBidAlertAsync(
            emailBids,
            cancellationToken);

        // Record successfully emailed bid versions.
        var sentOn = DateTime.Now;

        foreach (var candidate in unsentCandidates)
        {
            _dbContext.PuneBidAlertSents.Add(
                new PuneBidAlertSent
                {
                    BidId =
                        candidate.Bid.Id,

                    BidNumber =
                        candidate.Bid.BidNumber ?? string.Empty,

                    BidUpdatedOn =
                        candidate.Bid.UpdatedOn,

                    ChangeDetectedOn =
                        candidate.ChangeDetectedOn,

                    SentOn =
                        sentOn
                });
        }

        // Advance the checkpoint after successful email sending.
        state.LastCheckedAt = checkStartedAt;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Active IT Pune/Nilgiris/Leh bid alert processing completed. " +
            "Sent: {SentCount}. LastCheckedAt: {LastCheckedAt}.",
            unsentCandidates.Count,
            checkStartedAt);
    }

    private static string? GetMatchingLocation(
        GeMbidExtract bid)
    {
        // Match only against the Location column.
        if (string.IsNullOrWhiteSpace(bid.Location))
        {
            return null;
        }

        if (bid.Location.Equals(
                "Pune",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Pune";
        }

        if (bid.Location.Equals(
                "Nilgiris",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Nilgiris";
        }

        if (bid.Location.Equals(
                "Leh",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Leh";
        }

        return null;
    }
}