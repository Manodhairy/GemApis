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

        // Everything created/updated after this point
        // will be processed during the next check.
        var checkStartedAt = DateTime.Now;

        _logger.LogInformation(
            "Location bid alert check started. LastCheckedAt: {LastCheckedAt}",
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

        // Find bids where:
        // 1. CategoryKey is IT
        // 2. Location is Pune, Nilgiris, or Leh
        //
        // IMPORTANT:
        // Location is checked ONLY from the Location column.
        var matchingBids = changedBids
            .Where(bid =>
                string.Equals(
                    bid.CategoryKey,
                    "IT",
                    StringComparison.OrdinalIgnoreCase))
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
            "Found {PuneCount} IT Pune bid(s), {NilgirisCount} IT Nilgiris bid(s), and {LehCount} IT Leh bid(s).",
            puneBids.Count,
            nilgirisBids.Count,
            lehBids.Count);

        if (matchingBids.Count == 0)
        {
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "No IT Pune/Nilgiris/Leh bids found. LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Determine the timestamp representing this particular
        // bid version/change.
        var candidates = matchingBids
            .Select(x => new
            {
                Bid = x.Bid,
                Location = x.Location!,
                ChangeDetectedOn =
                    x.Bid.UpdatedOn ?? x.Bid.CreatedOn
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
            "{Count} IT Pune/Nilgiris/Leh bid change(s) require email notification.",
            unsentCandidates.Count);

        if (unsentCandidates.Count == 0)
        {
            state.LastCheckedAt = checkStartedAt;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "All IT Pune/Nilgiris/Leh bid changes were already notified. " +
                "LastCheckedAt updated to {CheckStartedAt}.",
                checkStartedAt);

            return;
        }

        // Convert database entities into the DTO containing
        // only the fields required by the email.
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

        // Only move the checkpoint after the email and audit
        // records have been successfully prepared.
        state.LastCheckedAt = checkStartedAt;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "IT Pune/Nilgiris/Leh bid alert processing completed. " +
            "Sent: {SentCount}. LastCheckedAt: {LastCheckedAt}.",
            unsentCandidates.Count,
            checkStartedAt);
    }

    private static string? GetMatchingLocation(
        GeMbidExtract bid)
    {
        // Only the Location column is checked.
        if (string.IsNullOrWhiteSpace(bid.Location))
        {
            return null;
        }

        // Case-insensitive exact match.
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