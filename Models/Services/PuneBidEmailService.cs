using GemApi.DTOs.Response;
using GemApi.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
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

        var client = new SendGridClient(_settings.ApiKey);

        var from = new EmailAddress(
            _settings.SenderEmail.Trim(),
            _settings.SenderName);

        var subject =
            $"Pune GeM Bid Alert - {bids.Count} Bid(s)";

        var message =
            MailHelper.CreateSingleEmailToMultipleRecipients(
                from,
                recipients,
                subject,
                plainText,
                html);

        _logger.LogInformation(
            "Sending Pune bid alert email for {Count} bid(s) to {RecipientCount} recipient(s).",
            bids.Count,
            recipients.Count);

        var response = await client.SendEmailAsync(
            message,
            cancellationToken);

        await EnsureSuccessAsync(response);

        _logger.LogInformation(
            "Pune bid alert email sent successfully for {Count} bid(s).",
            bids.Count);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "SendGrid API key is missing. " +
                "Check PuneBidAlert:ApiKey in User Secrets.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "Pune bid alert sender email is not configured. " +
                "Check PuneBidAlert:SenderEmail in appsettings.json.");
        }
    }

    private List<EmailAddress> GetRecipients()
    {
        var recipients = _settings.ReceiverEmails
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new EmailAddress(x.Trim()))
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
        var html = new StringBuilder();

        html.Append("""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>Pune GeM Bid Alert</title>

            <style>
                body {
                    margin: 0;
                    padding: 0;
                    background-color: #f3f4f6;
                    font-family: Arial, Helvetica, sans-serif;
                    color: #1f2937;
                }

                .container {
                    max-width: 900px;
                    margin: 30px auto;
                    background-color: #ffffff;
                    border-radius: 10px;
                    overflow: hidden;
                }

                .header {
                    background-color: #1d4ed8;
                    color: #ffffff;
                    padding: 25px;
                    text-align: center;
                }

                .header h1 {
                    margin: 0;
                    font-size: 25px;
                }

                .header p {
                    margin: 8px 0 0;
                    font-size: 14px;
                }

                .body {
                    padding: 25px;
                }

                .bid-table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 20px;
                    font-size: 13px;
                }

                .bid-table th {
                    background-color: #1d4ed8;
                    color: #ffffff;
                    padding: 10px;
                    border: 1px solid #d1d5db;
                    text-align: left;
                }

                .bid-table td {
                    padding: 10px;
                    border: 1px solid #d1d5db;
                    vertical-align: top;
                    word-break: break-word;
                }

                .bid-table tr:nth-child(even) {
                    background-color: #f9fafb;
                }

                .footer {
                    padding: 15px;
                    text-align: center;
                    background-color: #f9fafb;
                    color: #6b7280;
                    font-size: 12px;
                }

                @media only screen and (max-width: 700px) {
                    .container {
                        width: 100% !important;
                        margin: 0 !important;
                    }

                    .body {
                        padding: 12px !important;
                    }

                    .bid-table {
                        font-size: 11px;
                    }

                    .bid-table th,
                    .bid-table td {
                        padding: 6px;
                    }
                }
            </style>
        </head>

        <body>

            <div class="container">

                <div class="header">
                    <h1>Pune GeM Bid Alert</h1>
                    <p>New or updated Pune-related GeM bids</p>
                </div>

                <div class="body">

                    <p>
                        The following Pune-related GeM bid(s)
                        were detected:
                    </p>

                    <table class="bid-table">

                        <thead>
                            <tr>
                                <th>Bid Number</th>
                                <th>Card Start Date</th>
                                <th>Card End Date</th>
                                <th>Category</th>
                                <th>Office Name</th>
                                <th>Organisation Name</th>
                                <th>Consignee Name</th>
                            </tr>
                        </thead>

                        <tbody>
        """);

        foreach (var bid in bids)
        {
            html.Append("<tr>");

            html.Append(
                $"<td>{Encode(bid.BidNumber)}</td>");

            html.Append(
                $"<td>{Encode(bid.CardStartDate)}</td>");

            html.Append(
                $"<td>{Encode(bid.CardEndDate)}</td>");

            html.Append(
                $"<td>{Encode(bid.CategoryKey)}</td>");

            html.Append(
                $"<td>{Encode(bid.OfficeName)}</td>");

            html.Append(
                $"<td>{Encode(bid.OrganisationName)}</td>");

            html.Append(
                $"<td>{Encode(bid.ConsigneeName)}</td>");

            html.Append("</tr>");
        }

        html.Append("""
                        </tbody>

                    </table>

                    <p style="margin-top:25px;">
                        This is an automatically generated
                        Pune bid alert.
                    </p>

                </div>

                <div class="footer">
                    GeM Bid Alert System
                </div>

            </div>

        </body>
        </html>
        """);

        return html.ToString();
    }

    private static string BuildPlainText(
        IReadOnlyCollection<PuneBidAlertDto> bids)
    {
        var text = new StringBuilder();

        text.AppendLine("Pune GeM Bid Alert");
        text.AppendLine("==================");
        text.AppendLine();

        foreach (var bid in bids)
        {
            text.AppendLine(
                $"Bid Number: {bid.BidNumber}");

            text.AppendLine(
                $"Card Start Date: {bid.CardStartDate}");

            text.AppendLine(
                $"Card End Date: {bid.CardEndDate}");

            text.AppendLine(
                $"Category: {bid.CategoryKey}");

            text.AppendLine(
                $"Office Name: {bid.OfficeName}");

            text.AppendLine(
                $"Organisation Name: {bid.OrganisationName}");

            text.AppendLine(
                $"Consignee Name: {bid.ConsigneeName}");

            text.AppendLine();

            text.AppendLine(
                "------------------------------");

            text.AppendLine();
        }

        return text.ToString();
    }

    private static string Encode(string? value)
    {
        return HtmlEncoder.Default.Encode(
            value ?? string.Empty);
    }

    private static async Task EnsureSuccessAsync(
        Response response)
    {
        if ((int)response.StatusCode >= 400)
        {
            var responseBody =
                await response.Body.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"SendGrid returned {(int)response.StatusCode}: " +
                responseBody);
        }
    }
}