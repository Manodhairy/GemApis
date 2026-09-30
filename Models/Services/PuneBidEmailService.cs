using GemApi.DTOs.Response;
using GemApi.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;

namespace GemApi.Services;

public class PuneBidEmailService : IPuneBidEmailService
{
    private readonly PuneBidAlertSettings _settings;
    private readonly ILogger<PuneBidEmailService> _logger;

    public PuneBidEmailService(
        IOptions<PuneBidAlertSettings> options,
        ILogger<PuneBidEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendPuneBidAlertAsync(
        IReadOnlyCollection<PuneBidAlertDto> bids,
        CancellationToken cancellationToken)
    {
        if (bids == null || bids.Count == 0)
        {
            return;
        }

        ValidateSettings();

        var recipients = GetRecipients();

        var html = BuildEmailHtml(bids);

        var plainText = BuildPlainText(bids);

        using var message = new MailMessage();

        message.From = new MailAddress(
            _settings.SenderEmail.Trim(),
            _settings.SenderName);

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        message.Subject =
            $"Pune and Wellington Bids - {bids.Count} Bid(s)";

        /*
         * Create both versions of the email:
         *
         * 1. text/plain -> for clients that do not support HTML
         * 2. text/html  -> for clients that support HTML
         *
         * Do not use message.Body = html here.
         * The HTML content is added explicitly as an AlternateView.
         */

        var plainView = AlternateView.CreateAlternateViewFromString(
            plainText,
            Encoding.UTF8,
            "text/plain");

        var htmlView = AlternateView.CreateAlternateViewFromString(
            html,
            Encoding.UTF8,
            "text/html");

        message.AlternateViews.Add(plainView);
        message.AlternateViews.Add(htmlView);

        using var smtpClient = new SmtpClient(
            _settings.SmtpHost,
            _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _settings.SmtpUsername,
                _settings.SmtpPassword)
        };

        _logger.LogInformation(
            "Sending Pune/Wellington bid alert email for {Count} bid(s) to {RecipientCount} recipient(s) using Microsoft 365 SMTP.",
            bids.Count,
            recipients.Count);

        await smtpClient.SendMailAsync(
            message,
            cancellationToken);

        _logger.LogInformation(
            "Pune/Wellington bid alert email sent successfully for {Count} bid(s).",
            bids.Count);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "Pune bid alert sender email is not configured. " +
                "Check PuneBidAlert:SenderEmail in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            throw new InvalidOperationException(
                "SMTP host is not configured. " +
                "Check PuneBidAlert:SmtpHost in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpUsername))
        {
            throw new InvalidOperationException(
                "SMTP username is not configured. " +
                "Check PuneBidAlert:SmtpUsername in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            throw new InvalidOperationException(
                "SMTP password is missing. " +
                "Check PuneBidAlert:SmtpPassword in User Secrets.");
        }

        if (_settings.SmtpPort <= 0)
        {
            throw new InvalidOperationException(
                "SMTP port is invalid.");
        }
    }

    private List<string> GetRecipients()
    {
        var recipients = _settings.ReceiverEmails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException(
                "No Pune bid alert receiver emails configured. " +
                "Check PuneBidAlert:ReceiverEmails in appsettings.json.");
        }

        return recipients;
    }

    private static string BuildEmailHtml(
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        var puneBids = bids
            .Where(x =>
                string.Equals(
                    x.Location,
                    "Pune",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var wellingtonBids = bids
            .Where(x =>
                string.Equals(
                    x.Location,
                    "Wellington",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var html = new StringBuilder();

        html.Append("""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">

            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>Pune and Wellington Bids</title>

            <style>

                html,
                body {
                    margin: 0;
                    padding: 0;
                    width: 100%;
                    background-color: #f3f4f6;
                }

                body {
                    font-family: Arial, Helvetica, sans-serif;
                    color: #1f2937;
                }

                .container {
                    width: 100%;
                    max-width: 900px;
                    margin: 20px auto;
                    background-color: #ffffff;
                }

                .header {
                    background-color: #1d4ed8;
                    color: #ffffff;
                    padding: 20px 15px;
                    text-align: center;
                }

                .header h1 {
                    margin: 0;
                    font-size: 22px;
                    line-height: 28px;
                }

                .summary {
                    padding: 15px;
                    text-align: center;
                    font-size: 13px;
                    color: #4b5563;
                }

                .section {
                    padding: 0 12px 18px 12px;
                }

                .section-title {
                    margin: 0 0 8px 0;
                    padding: 9px 10px;
                    background-color: #f3f4f6;
                    border-left: 4px solid #1d4ed8;
                    font-size: 15px;
                    font-weight: bold;
                    color: #111827;
                }

                .bid-table {
                    width: 100%;
                    border-collapse: collapse;
                    table-layout: fixed;
                    font-size: 11px;
                }

                .bid-table th {
                    background-color: #1d4ed8;
                    color: #ffffff;
                    padding: 7px 5px;
                    text-align: left;
                    font-size: 10px;
                    font-weight: bold;
                    border-right: 1px solid #ffffff;
                }

                .bid-table th:last-child {
                    border-right: none;
                }

                .bid-table td {
                    padding: 7px 5px;
                    border-bottom: 1px solid #e5e7eb;
                    vertical-align: middle;
                    word-break: break-word;
                }

                .bid-table tbody tr:nth-child(even) {
                    background-color: #f9fafb;
                }

                .bid-number {
                    font-weight: bold;
                }

                .bid-number a {
                    color: #1d4ed8;
                    text-decoration: none;
                    font-weight: bold;
                }

                .bid-number a:hover {
                    text-decoration: underline;
                }

                .category {
                    word-break: break-word;
                }

                .date {
                    white-space: nowrap;
                    font-size: 10px;
                }

                .empty-message {
                    padding: 10px;
                    color: #6b7280;
                    font-size: 12px;
                    text-align: center;
                    border: 1px solid #e5e7eb;
                }

                .footer {
                    padding: 12px;
                    text-align: center;
                    background-color: #f9fafb;
                    color: #6b7280;
                    font-size: 10px;
                }

                @media only screen and (max-width: 600px) {

                    .container {
                        width: 100% !important;
                        margin: 0 !important;
                    }

                    .header {
                        padding: 16px 10px !important;
                    }

                    .header h1 {
                        font-size: 19px !important;
                        line-height: 24px !important;
                    }

                    .summary {
                        padding: 12px 8px !important;
                        font-size: 12px !important;
                    }

                    .section {
                        padding-left: 6px !important;
                        padding-right: 6px !important;
                        padding-bottom: 14px !important;
                    }

                    .section-title {
                        font-size: 14px !important;
                        padding: 8px !important;
                    }

                    .bid-table {
                        font-size: 9px !important;
                    }

                    .bid-table th {
                        padding: 6px 3px !important;
                        font-size: 8px !important;
                    }

                    .bid-table td {
                        padding: 6px 3px !important;
                    }

                    .date {
                        font-size: 8px !important;
                    }

                    .footer {
                        font-size: 9px !important;
                    }
                }

            </style>
        </head>

        <body>

            <div class="container">

                <div class="header">
                    <h1>Pune and Wellington Bids</h1>
                </div>

                <div class="summary">
        """);

        html.Append(
            $"<strong>{puneBids.Count}</strong> Pune bid(s) " +
            $"&nbsp;&nbsp;•&nbsp;&nbsp; " +
            $"<strong>{wellingtonBids.Count}</strong> Wellington bid(s)");

        html.Append("""
                </div>
        """);

        // Pune section
        html.Append("""
                <div class="section">

                    <div class="section-title">
                        📍 Pune Bids
                    </div>
        """);

        AppendBidTable(
            html,
            puneBids);

        html.Append("""
                </div>
        """);

        // Wellington section
        html.Append("""
                <div class="section">

                    <div class="section-title">
                        🌍 Wellington Bids
                    </div>
        """);

        AppendBidTable(
            html,
            wellingtonBids);

        html.Append("""
                </div>

                <div class="footer">
                    Automatically generated by GeM Bid Alert System
                </div>

            </div>

        </body>
        </html>
        """);

        return html.ToString();
    }

    private static void AppendBidTable(
        StringBuilder html,
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        if (bids.Count == 0)
        {
            html.Append("""
                    <div class="empty-message">
                        No bids detected.
                    </div>
            """);

            return;
        }

        html.Append("""
                    <table class="bid-table">

                        <colgroup>
                            <col style="width: 38%;">
                            <col style="width: 22%;">
                            <col style="width: 20%;">
                            <col style="width: 20%;">
                        </colgroup>

                        <thead>
                            <tr>
                                <th>Bid Number</th>
                                <th>Category</th>
                                <th>Start Date</th>
                                <th>End Date</th>
                            </tr>
                        </thead>

                        <tbody>
        """);

        foreach (var bid in bids)
        {
            html.Append("<tr>");

            /*
             * Bid Number
             *
             * If PdfUrl exists, make the Bid Number clickable.
             * If PdfUrl is missing, display the Bid Number normally.
             */

            if (!string.IsNullOrWhiteSpace(bid.PdfUrl))
            {
                var encodedUrl =
                    EncodeAttribute(bid.PdfUrl);

                var encodedBidNumber =
                    Encode(bid.BidNumber);

                html.Append(
                    $"<td class=\"bid-number\">" +
                    $"<a href=\"{encodedUrl}\" " +
                    $"target=\"_blank\">" +
                    $"{encodedBidNumber}" +
                    $"</a>" +
                    $"</td>");
            }
            else
            {
                html.Append(
                    $"<td class=\"bid-number\">" +
                    $"{Encode(bid.BidNumber)}" +
                    $"</td>");
            }

            // Category
            html.Append(
                $"<td class=\"category\">" +
                $"{Encode(bid.CategoryKey)}" +
                $"</td>");

            // Start Date
            html.Append(
                $"<td class=\"date\">" +
                $"{Encode(FormatDate(bid.CardStartDate))}" +
                $"</td>");

            // End Date
            html.Append(
                $"<td class=\"date\">" +
                $"{Encode(FormatDate(bid.CardEndDate))}" +
                $"</td>");

            html.Append("</tr>");
        }

        html.Append("""
                        </tbody>

                    </table>
        """);
    }

    private static string BuildPlainText(
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        var puneBids = bids
            .Where(x =>
                string.Equals(
                    x.Location,
                    "Pune",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var wellingtonBids = bids
            .Where(x =>
                string.Equals(
                    x.Location,
                    "Wellington",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var text = new StringBuilder();

        text.AppendLine("Pune and Wellington Bids");
        text.AppendLine("========================");
        text.AppendLine();

        text.AppendLine(
            $"Pune Bids: {puneBids.Count}");

        text.AppendLine(
            $"Wellington Bids: {wellingtonBids.Count}");

        text.AppendLine();

        if (puneBids.Count > 0)
        {
            text.AppendLine("PUNE BIDS");
            text.AppendLine("---------");

            AppendPlainTextBids(
                text,
                puneBids);

            text.AppendLine();
        }

        if (wellingtonBids.Count > 0)
        {
            text.AppendLine("WELLINGTON BIDS");
            text.AppendLine("----------------");

            AppendPlainTextBids(
                text,
                wellingtonBids);
        }

        return text.ToString();
    }

    private static void AppendPlainTextBids(
        StringBuilder text,
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        foreach (var bid in bids)
        {
            text.AppendLine(
                $"Bid Number: {bid.BidNumber}");

            text.AppendLine(
                $"Category: {bid.CategoryKey}");

            text.AppendLine(
                $"Start Date: {FormatDate(bid.CardStartDate)}");

            text.AppendLine(
                $"End Date: {FormatDate(bid.CardEndDate)}");

            if (!string.IsNullOrWhiteSpace(bid.PdfUrl))
            {
                text.AppendLine(
                    $"PDF: {bid.PdfUrl}");
            }

            text.AppendLine();
        }
    }

    private static string FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(
                value,
                out var date))
        {
            return date.ToString(
                "dd-MMM-yyyy HH:mm");
        }

        return value;
    }

    private static string Encode(string? value)
    {
        return HtmlEncoder.Default.Encode(
            value ?? string.Empty);
    }

    private static string EncodeAttribute(string? value)
    {
        return HtmlEncoder.Default.Encode(
            value ?? string.Empty);
    }
}