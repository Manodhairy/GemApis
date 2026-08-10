using GemApi.DTOs.Response;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;
using System.Text.Encodings.Web;

namespace GemApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendBidNotificationAsync(
            BidNotificationSummaryDto summary,
            int minimumRecordCount)
        {
            try
            {
                // ============================================
                // BUILD HTML EMAIL
                // ============================================

                var html = new StringBuilder();

                html.Append("""
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="UTF-8">
                        <meta name="viewport"
                              content="width=device-width, initial-scale=1.0">
                        <title>GeM Bid Alert</title>
                    </head>

                    <body style="
                        margin:0;
                        padding:20px;
                        background-color:#f3f4f6;
                        font-family:Arial, sans-serif;
                        color:#1f2937;">

                        <div style="
                            max-width:750px;
                            margin:0 auto;
                            background-color:#ffffff;
                            border-radius:12px;
                            overflow:hidden;
                            box-shadow:0 4px 12px rgba(0,0,0,0.08);">

                            <!-- Header -->

                            <div style="
                                background-color:#1d4ed8;
                                color:#ffffff;
                                padding:24px;
                                text-align:center;">

                                <h1 style="
                                    margin:0;
                                    font-size:26px;">
                                    GeM Bid Alert
                                </h1>

                                <p style="
                                    margin:8px 0 0;
                                    font-size:15px;">
                                    New bid records notification
                                </p>

                            </div>

                            <!-- Body -->

                            <div style="padding:25px;">

                                <p style="
                                    margin-top:0;
                                    font-size:16px;">
                                    Hello,
                                </p>
                    """);

                // ============================================
                // SUMMARY
                // ============================================

                html.Append($"""
                                <p style="font-size:16px;">
                                    <strong>{summary.NewRecordCount}</strong>
                                    new GeM bid records have been added.
                                </p>

                                <h2 style="
                                    margin-top:25px;
                                    font-size:19px;
                                    color:#1d4ed8;">
                                    Summary
                                </h2>

                                <table style="
                                    width:100%;
                                    border-collapse:collapse;
                                    margin-top:12px;
                                    font-size:14px;">

                                    <tbody>

                                        <tr>
                                            <td style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                background:#eff6ff;
                                                font-weight:bold;">
                                                New records added
                                            </td>

                                            <td style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                color:#15803d;
                                                font-weight:bold;">
                                                {summary.NewRecordCount}
                                            </td>
                                        </tr>

                                        <tr>
                                            <td style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                background:#eff6ff;
                                                font-weight:bold;">
                                                Total records
                                            </td>

                                            <td style="
                                                padding:12px;
                                                border:1px solid #d1d5db;">
                                                {summary.TotalRecordCount}
                                            </td>
                                        </tr>

                                    </tbody>
                                </table>
                    """);

                // ============================================
                // CATEGORY TABLE
                // ============================================

                html.Append("""
                                <h2 style="
                                    margin-top:30px;
                                    font-size:19px;
                                    color:#1d4ed8;">
                                    Category and Subcategory Count
                                </h2>

                                <table style="
                                    width:100%;
                                    border-collapse:collapse;
                                    margin-top:12px;
                                    font-size:14px;">

                                    <thead>

                                        <tr style="
                                            background-color:#1d4ed8;
                                            color:#ffffff;">

                                            <th style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                text-align:left;">
                                                Category
                                            </th>

                                            <th style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                text-align:left;">
                                                Subcategory
                                            </th>

                                            <th style="
                                                padding:12px;
                                                border:1px solid #d1d5db;
                                                text-align:center;">
                                                Count
                                            </th>

                                        </tr>

                                    </thead>

                                    <tbody>
                    """);

                // ============================================
                // CATEGORY DATA
                // ============================================

                foreach (var category in summary.CategoryCounts)
                {
                    var categoryKey =
                        HtmlEncoder.Default.Encode(
                            category.CategoryKey ?? string.Empty);

                    var categorySubKey =
                        HtmlEncoder.Default.Encode(
                            category.CategorySubKey ?? string.Empty);

                    html.Append($"""
                                        <tr>

                                            <td style="
                                                padding:11px;
                                                border:1px solid #d1d5db;">
                                                {categoryKey}
                                            </td>

                                            <td style="
                                                padding:11px;
                                                border:1px solid #d1d5db;">
                                                {categorySubKey}
                                            </td>

                                            <td style="
                                                padding:11px;
                                                border:1px solid #d1d5db;
                                                text-align:center;
                                                font-weight:bold;">
                                                {category.Count}
                                            </td>

                                        </tr>
                        """);
                }

                // ============================================
                // FOOTER
                // ============================================

                html.Append("""
                                    </tbody>

                                </table>

                                <p style="
                                    margin-top:30px;
                                    margin-bottom:0;
                                    color:#4b5563;">

                                    Thank you,<br>

                                    <strong>
                                        GeM Bid Alert System
                                    </strong>

                                </p>

                            </div>

                            <div style="
                                padding:15px;
                                text-align:center;
                                background-color:#f9fafb;
                                color:#6b7280;
                                font-size:12px;">

                                This is an automatically generated email.

                            </div>

                        </div>

                    </body>
                    </html>
                    """);

                // ============================================
                // VALIDATE API KEY
                // ============================================

                if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                {
                    throw new InvalidOperationException(
                        "SendGrid API key is missing. " +
                        "Check EmailSettings:ApiKey in User Secrets."
                    );
                }

                // ============================================
                // VALIDATE SENDER
                // ============================================

                if (string.IsNullOrWhiteSpace(
                    _settings.SenderEmail))
                {
                    throw new InvalidOperationException(
                        "Sender email is not configured."
                    );
                }

                // ============================================
                // CREATE SENDGRID CLIENT
                // ============================================

                var client =
                    new SendGridClient(_settings.ApiKey);

                // ============================================
                // FROM
                // ============================================

                var from = new EmailAddress(
                    _settings.SenderEmail.Trim(),
                    _settings.SenderName
                );

                // ============================================
                // SUBJECT
                // ============================================

                var subject =
                    $"{summary.NewRecordCount} new GeM bids added";

                // ============================================
                // RECIPIENTS
                // ============================================

                var toList = _settings.ReceiverEmails
                    .Where(r =>
                        !string.IsNullOrWhiteSpace(r))
                    .Select(r =>
                        new EmailAddress(r.Trim()))
                    .ToList();

                if (toList.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No receiver emails configured."
                    );
                }

                // ============================================
                // CREATE EMAIL
                // ============================================

                var msg =
                    MailHelper.CreateSingleEmailToMultipleRecipients(
                        from,
                        toList,
                        subject,
                        plainTextContent:
                            $"{summary.NewRecordCount} " +
                            "new GeM bid records have been added.",
                        htmlContent:
                            html.ToString()
                    );

                // ============================================
                // SEND EMAIL
                // ============================================

                _logger.LogInformation(
                    "Sending GeM bid email to {Count} recipients.",
                    toList.Count
                );

                var response =
                    await client.SendEmailAsync(msg);

                // ============================================
                // CHECK RESPONSE
                // ============================================

                if ((int)response.StatusCode >= 400)
                {
                    var responseBody =
                        await response.Body.ReadAsStringAsync();

                    _logger.LogError(
                        "SendGrid failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseBody
                    );

                    throw new InvalidOperationException(
                        $"SendGrid returned " +
                        $"{(int)response.StatusCode}: " +
                        $"{responseBody}"
                    );
                }

                _logger.LogInformation(
                    "GeM bid email sent successfully to {Count} recipients.",
                    toList.Count
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send GeM bid notification email."
                );

                throw;
            }
        }
    }
}