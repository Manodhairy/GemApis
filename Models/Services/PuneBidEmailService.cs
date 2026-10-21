
using GemApi.DTOs.Response;
using GemApi.Settings;
using Microsoft.Extensions.Options;
using System.Globalization;
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
            _logger.LogInformation(
                "No bids supplied for the email alert. Email will not be sent.");
            return;
        }

        ValidateSettings();

        var recipients = GetRecipients();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException(
                "No receiver email addresses are configured in PuneBidAlert:ReceiverEmails.");
        }

        var html = BuildEmailHtml(bids);
        var plainText = BuildPlainText(bids);

        using var message = new MailMessage
        {
            From = new MailAddress(
                _settings.SenderEmail.Trim(),
                _settings.SenderName),
            Subject = $"Pune, Nilgiris and Leh Bids - {bids.Count} Bid(s)",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Body = plainText,
            IsBodyHtml = false
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        if (!string.IsNullOrWhiteSpace(_settings.ReplyToEmail))
        {
            message.ReplyToList.Add(
                new MailAddress(_settings.ReplyToEmail.Trim()));
        }

        message.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                plainText,
                Encoding.UTF8,
                "text/plain"));

        message.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(
                html,
                Encoding.UTF8,
                "text/html"));

        using var smtpClient = new SmtpClient(
            _settings.SmtpHost.Trim(),
            _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _settings.SmtpUsername.Trim(),
                _settings.SmtpPassword)
        };

        _logger.LogInformation(
            "Sending Pune/Nilgiris/Leh bid alert for {Count} bid(s) to {RecipientCount} recipient(s) using Brevo SMTP.",
            bids.Count,
            recipients.Count);

        await smtpClient.SendMailAsync(message, cancellationToken);

        _logger.LogInformation(
            "Pune/Nilgiris/Leh bid alert email sent successfully for {Count} bid(s).",
            bids.Count);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "PuneBidAlert:SenderEmail is missing from configuration.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            throw new InvalidOperationException(
                "PuneBidAlert:SmtpHost is missing from configuration.");
        }

        if (_settings.SmtpPort <= 0 || _settings.SmtpPort > 65535)
        {
            throw new InvalidOperationException(
                "PuneBidAlert:SmtpPort must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpUsername))
        {
            throw new InvalidOperationException(
                "PuneBidAlert:SmtpUsername is missing from configuration.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            throw new InvalidOperationException(
                "PuneBidAlert:SmtpPassword is missing from configuration.");
        }

        if (_settings.ReceiverEmails == null ||
            !_settings.ReceiverEmails.Any(
                email => !string.IsNullOrWhiteSpace(email)))
        {
            throw new InvalidOperationException(
                "Configure at least one email address in PuneBidAlert:ReceiverEmails.");
        }
    }

    private List<string> GetRecipients()
    {
        return _settings.ReceiverEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildEmailHtml(
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Pune, Nilgiris and Leh Bids</title>
                <style>
                    body {
                        margin: 0;
                        padding: 16px;
                        font-family: Arial, Helvetica, sans-serif;
                        color: #222222;
                        background-color: #ffffff;
                    }

                    .email-container {
                        width: 100%;
                        max-width: 900px;
                        margin: 0 auto;
                    }

                    h2 {
                        margin: 0 0 22px 0;
                        font-size: 22px;
                        line-height: 1.4;
                    }

                    h3 {
                        margin: 24px 0 10px 0;
                        font-size: 17px;
                    }

                    .table-wrapper {
                        width: 100%;
                        overflow-x: auto;
                        margin-bottom: 20px;
                    }

                    table {
                        width: 100%;
                        border-collapse: collapse;
                        table-layout: fixed;
                        font-size: 13px;
                    }

                    th, td {
                        border: 1px solid #d9d9d9;
                        padding: 10px 8px;
                        text-align: left;
                        vertical-align: top;
                        overflow-wrap: anywhere;
                        word-break: normal;
                    }

                    th {
                        background-color: #f2f4f7;
                        font-weight: bold;
                    }

                    th:nth-child(1), td:nth-child(1) { width: 29%; }
                    th:nth-child(2), td:nth-child(2) { width: 35%; }
                    th:nth-child(3), td:nth-child(3) { width: 18%; }
                    th:nth-child(4), td:nth-child(4) { width: 18%; }

                    a {
                        color: #0563c1;
                        text-decoration: underline;
                        overflow-wrap: anywhere;
                    }

                    .empty-message {
                        color: #555555;
                        font-size: 13px;
                    }

                    @media only screen and (max-width: 600px) {
                        body {
                            padding: 8px;
                        }

                        h2 {
                            font-size: 19px;
                        }

                        h3 {
                            font-size: 15px;
                        }

                        table {
                            font-size: 11px;
                        }

                        th, td {
                            padding: 7px 5px;
                        }
                    }
                </style>
            </head>
            <body>
            <div class="email-container">
            <h2>Pune, Nilgiris and Leh Bids</h2>
            """);

        AppendBidTable(sb, bids, "Pune", "📍 Pune Bids");
        AppendBidTable(sb, bids, "Nilgiris", "📍 Nilgiris Bids");
        AppendBidTable(sb, bids, "Leh", "📍 Leh Bids");

        sb.Append("""
            </div>
            </body>
            </html>
            """);

        return sb.ToString();
    }

    private static void AppendBidTable(
        StringBuilder sb,
        IReadOnlyCollection<PuneBidAlertDto> bids,
        string location,
        string sectionTitle)
    {
        var locationBids = bids
            .Where(bid => string.Equals(
                bid.Location?.Trim(),
                location,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (locationBids.Count == 0)
        {
            return;
        }

        sb.Append("<h3>");
        sb.Append(Encode(sectionTitle));
        sb.Append("</h3>");

        sb.Append("""
            <div class="table-wrapper">
            <table>
                <thead>
                    <tr>
                        <th>Bid Number</th>
                        <th>Item Category</th>
                        <th>Start Date</th>
                        <th>End Date</th>
                    </tr>
                </thead>
                <tbody>
            """);

        foreach (var bid in locationBids)
        {
            sb.Append("<tr>");

            sb.Append("<td>");

            if (!string.IsNullOrWhiteSpace(bid.PdfUrl) &&
                Uri.TryCreate(
                    bid.PdfUrl.Trim(),
                    UriKind.Absolute,
                    out var pdfUri) &&
                (pdfUri.Scheme == Uri.UriSchemeHttp ||
                 pdfUri.Scheme == Uri.UriSchemeHttps))
            {
                sb.Append("<a href=\"");
                sb.Append(EncodeAttribute(pdfUri.AbsoluteUri));
                sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
                sb.Append(Encode(bid.BidNumber));
                sb.Append("</a>");
            }
            else
            {
                sb.Append(Encode(bid.BidNumber));
            }

            sb.Append("</td>");

            sb.Append("<td>");
            sb.Append(Encode(bid.ItemCategory));
            sb.Append("</td>");

            sb.Append("<td>");
            sb.Append(Encode(FormatDate(bid.CardStartDate)));
            sb.Append("</td>");

            sb.Append("<td>");
            sb.Append(Encode(FormatDate(bid.CardEndDate)));
            sb.Append("</td>");

            sb.Append("</tr>");
        }

        sb.Append("""
                </tbody>
            </table>
            </div>
            """);
    }

    private static string BuildPlainText(
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Pune, Nilgiris and Leh Bids");
        sb.AppendLine(new string('=', 35));
        sb.AppendLine();

        AppendPlainTextBids(sb, bids, "Pune");
        AppendPlainTextBids(sb, bids, "Nilgiris");
        AppendPlainTextBids(sb, bids, "Leh");

        return sb.ToString();
    }

    private static void AppendPlainTextBids(
        StringBuilder sb,
        IReadOnlyCollection<PuneBidAlertDto> bids,
        string location)
    {
        var locationBids = bids
            .Where(bid => string.Equals(
                bid.Location?.Trim(),
                location,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (locationBids.Count == 0)
        {
            return;
        }

        sb.AppendLine($"📍 {location} Bids");
        sb.AppendLine(new string('-', 30));

        foreach (var bid in locationBids)
        {
            sb.AppendLine($"Bid Number: {bid.BidNumber}");
            sb.AppendLine($"Category: {bid.CategoryKey}");
            sb.AppendLine($"Item Category: {bid.ItemCategory}");
            sb.AppendLine($"Start Date: {FormatDate(bid.CardStartDate)}");
            sb.AppendLine($"End Date: {FormatDate(bid.CardEndDate)}");

            if (!string.IsNullOrWhiteSpace(bid.PdfUrl))
            {
                sb.AppendLine($"PDF: {bid.PdfUrl}");
            }

            sb.AppendLine();
        }
    }

    private static string FormatDate(object? value)
    {
        if (value == null)
        {
            return "N/A";
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString(
                "dd-MMM-yyyy",
                CultureInfo.InvariantCulture);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(text))
        {
            return "N/A";
        }

        if (DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var parsedDate))
        {
            return parsedDate.ToString(
                "dd-MMM-yyyy",
                CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string Encode(string? value)
    {
        return HtmlEncoder.Default.Encode(value ?? string.Empty);
    }

    private static string EncodeAttribute(string? value)
    {
        return HtmlEncoder.Default.Encode(value ?? string.Empty);
    }
}
