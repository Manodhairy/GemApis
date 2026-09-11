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
        #region Field
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        #endregion

        #region Constructor
        public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }
        #endregion SendBidNotificationAsync

        #region SendBidNotificationAsync
        public async Task SendBidNotificationAsync(BidNotificationSummaryDto summary, int minimumRecordCount)
        {
            try
            {
                var html = BuildEmailHtml(summary);

                ValidateSettings();

                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.SenderEmail.Trim(), _settings.SenderName);
                var subject = $"{summary.NewRecordCount} new GeM bids added";
                var toList = GetRecipients();

                var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                    from,
                    toList,
                    subject,
                    plainTextContent: $"{summary.NewRecordCount} new GeM bid records have been added.",
                    htmlContent: html);

                _logger.LogInformation("Sending GeM bid email to {Count} recipients.", toList.Count);

                var response = await client.SendEmailAsync(msg);

                await EnsureSuccessAsync(response);

                _logger.LogInformation("GeM bid email sent successfully to {Count} recipients.", toList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send GeM bid notification email.");
                throw;
            }
        }

        #endregion

        #region VALIDATION


        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new InvalidOperationException(
                    "SendGrid API key is missing. Check EmailSettings:ApiKey in User Secrets.");
            }

            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                throw new InvalidOperationException("Sender email is not configured.");
            }
        }

        private List<EmailAddress> GetRecipients()
        {
            var toList = _settings.ReceiverEmails
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => new EmailAddress(r.Trim()))
                .ToList();

            if (toList.Count == 0)
            {
                throw new InvalidOperationException("No receiver emails configured.");
            }

            return toList;
        }

        private async Task EnsureSuccessAsync(Response response)
        {
            if ((int)response.StatusCode >= 400)
            {
                var responseBody = await response.Body.ReadAsStringAsync();

                _logger.LogError(
                    "SendGrid failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    responseBody);

                throw new InvalidOperationException(
                    $"SendGrid returned {(int)response.StatusCode}: {responseBody}");
            }
        }

        #endregion

        #region HTML BUILDING


        private string BuildEmailHtml(BidNotificationSummaryDto summary)
        {
            var html = new StringBuilder();

            html.Append(BuildHeaderAndOpeningSection());
            html.Append(BuildSummarySection(summary));
            html.Append(BuildCategoryTableHeader());
            html.Append(BuildCategoryRows(summary));
            html.Append(BuildFooterSection());

            return html.ToString();
        }

        private static string BuildHeaderAndOpeningSection()
        {
            return """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>GeM Bid Alert</title>

            <style>
                @media only screen and (max-width: 600px) {

                    .email-wrapper {
                        width: 100% !important;
                        padding: 10px !important;
                    }

                    .email-container {
                        width: 100% !important;
                        border-radius: 8px !important;
                    }

                    .email-header {
                        padding: 20px 15px !important;
                    }

                    .email-header h1 {
                        font-size: 22px !important;
                    }

                    .email-body {
                        padding: 18px 14px !important;
                    }

                    .email-body p {
                        font-size: 14px !important;
                    }

                    .summary-table td {
                        padding: 10px 8px !important;
                        font-size: 13px !important;
                    }

                    .category-table th,
                    .category-table td {
                        padding: 8px 6px !important;
                        font-size: 12px !important;
                    }

                    .category-table th:nth-child(1),
                    .category-table td:nth-child(1) {
                        width: 35% !important;
                    }

                    .category-table th:nth-child(2),
                    .category-table td:nth-child(2) {
                        width: 45% !important;
                    }

                    .category-table th:nth-child(3),
                    .category-table td:nth-child(3) {
                        width: 20% !important;
                    }

                    .footer {
                        padding: 12px !important;
                        font-size: 11px !important;
                    }
                }
            </style>
        </head>

        <body style="
            margin:0;
            padding:0;
            background-color:#f3f4f6;
            font-family:Arial,Helvetica,sans-serif;
            color:#1f2937;
        ">

            <div class="email-wrapper"
                 style="
                    width:100%;
                    padding:25px 10px;
                    box-sizing:border-box;
                 ">

                <div class="email-container"
                     style="
                        max-width:750px;
                        width:100%;
                        margin:0 auto;
                        background-color:#ffffff;
                        border-radius:12px;
                        overflow:hidden;
                        box-shadow:0 4px 12px rgba(0,0,0,0.08);
                     ">

                    <!-- HEADER -->

                    <div class="email-header"
                         style="
                            background-color:#1d4ed8;
                            color:#ffffff;
                            padding:25px 20px;
                            text-align:center;
                         ">

                        <h1 style="
                            margin:0;
                            font-size:26px;
                            line-height:1.3;
                        ">
                            GeM Bid Alert
                        </h1>

                        <p style="
                            margin:8px 0 0;
                            font-size:15px;
                            line-height:1.5;
                        ">
                            New bid records notification
                        </p>

                    </div>

                    <!-- BODY -->

                    <div class="email-body"
                         style="
                            padding:25px;
                            box-sizing:border-box;
                         ">

                        <p style="
                            margin:0 0 15px;
                            font-size:16px;
                            line-height:1.6;
                        ">
                            Hello,
                        </p>
        """;
        }


        private static string BuildSummarySection(
            BidNotificationSummaryDto summary)
        {
            return $"""
                    <p style="
                        margin:0 0 20px;
                        font-size:16px;
                        line-height:1.6;
                    ">
                        <strong>{summary.NewRecordCount}</strong>
                        new GeM bid records have been added.
                    </p>

                    <h2 style="
                        margin:25px 0 12px;
                        font-size:19px;
                        line-height:1.4;
                        color:#1d4ed8;
                    ">
                        Summary
                    </h2>

                    <table class="summary-table"
                           width="100%"
                           cellpadding="0"
                           cellspacing="0"
                           border="0"
                           style="
                                width:100%;
                                border-collapse:collapse;
                                font-size:14px;
                           ">

                        <tbody>

                            <tr>

                                <td style="
                                    padding:12px;
                                    border:1px solid #d1d5db;
                                    background-color:#eff6ff;
                                    font-weight:bold;
                                ">
                                    New records added
                                </td>

                                <td style="
                                    padding:12px;
                                    border:1px solid #d1d5db;
                                    color:#15803d;
                                    font-weight:bold;
                                    text-align:right;
                                ">
                                    {summary.NewRecordCount}
                                </td>

                            </tr>

                            <tr>

                                <td style="
                                    padding:12px;
                                    border:1px solid #d1d5db;
                                    background-color:#eff6ff;
                                    font-weight:bold;
                                ">
                                    Total records
                                </td>

                                <td style="
                                    padding:12px;
                                    border:1px solid #d1d5db;
                                    text-align:right;
                                ">
                                    {summary.TotalRecordCount}
                                </td>

                            </tr>

                        </tbody>

                    </table>
        """;
        }


        private static string BuildCategoryTableHeader()
        {
            return """
                    <h2 style="
                        margin:30px 0 12px;
                        font-size:19px;
                        line-height:1.4;
                        color:#1d4ed8;
                    ">
                        Category and Subcategory Count
                    </h2>

                    <div style="
                        width:100%;
                        overflow-x:auto;
                    ">

                        <table class="category-table"
                               width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0"
                               style="
                                    width:100%;
                                    border-collapse:collapse;
                                    table-layout:fixed;
                                    font-size:14px;
                               ">

                            <thead>

                                <tr style="
                                    background-color:#1d4ed8;
                                    color:#ffffff;
                                ">

                                    <th style="
                                        width:35%;
                                        padding:12px 8px;
                                        border:1px solid #d1d5db;
                                        text-align:left;
                                        word-break:break-word;
                                    ">
                                        Category
                                    </th>

                                    <th style="
                                        width:45%;
                                        padding:12px 8px;
                                        border:1px solid #d1d5db;
                                        text-align:left;
                                        word-break:break-word;
                                    ">
                                        Subcategory
                                    </th>

                                    <th style="
                                        width:20%;
                                        padding:12px 8px;
                                        border:1px solid #d1d5db;
                                        text-align:center;
                                    ">
                                        Count
                                    </th>

                                </tr>

                            </thead>

                            <tbody>
        """;
        }


        private static string BuildCategoryRows(
            BidNotificationSummaryDto summary)
        {
            var rows = new StringBuilder();

            foreach (var category in summary.CategoryCounts)
            {
                var categoryKey =
                    HtmlEncoder.Default.Encode(
                        category.CategoryKey ?? string.Empty);

                var categorySubKey =
                    HtmlEncoder.Default.Encode(
                        category.CategorySubKey ?? string.Empty);

                rows.Append($"""
                            <tr>

                                <td style="
                                    padding:11px 8px;
                                    border:1px solid #d1d5db;
                                    word-break:break-word;
                                    overflow-wrap:anywhere;
                                ">
                                    {categoryKey}
                                </td>

                                <td style="
                                    padding:11px 8px;
                                    border:1px solid #d1d5db;
                                    word-break:break-word;
                                    overflow-wrap:anywhere;
                                ">
                                    {categorySubKey}
                                </td>

                                <td style="
                                    padding:11px 8px;
                                    border:1px solid #d1d5db;
                                    text-align:center;
                                    font-weight:bold;
                                    white-space:nowrap;
                                ">
                                    {category.Count}
                                </td>

                            </tr>
            """);
            }

            return rows.ToString();
        }


        private static string BuildFooterSection()
        {
            return """
                            </tbody>

                        </table>

                    </div>

                    <p style="
                        margin:30px 0 0;
                        color:#4b5563;
                        font-size:14px;
                        line-height:1.6;
                    ">
                        Thank you,<br>
                        <strong>GeM Bid Alert System</strong>
                    </p>

                </div>

                <!-- FOOTER -->

                <div class="footer"
                     style="
                        padding:15px;
                        text-align:center;
                        background-color:#f9fafb;
                        color:#6b7280;
                        font-size:12px;
                        line-height:1.5;
                     ">

                    This is an automatically generated email.

                </div>

            </div>


        </body>
        </html>
        """;
        }

        #endregion


    }
}