using GemApi.DTOs.Response;
using GemApi.Services.Interfaces;
using GemApi.Settings;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Options;

using MimeKit;

using System.Text;
using System.Text.Encodings.Web;

namespace GemApi.Services
{
    public class EmailService : IEmailService
    {
        #region Fields

        private readonly EmailSettings _settings;

        private readonly ILogger<EmailService> _logger;

        #endregion


        #region Constructor

        public EmailService(
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger)
        {
            _settings = options.Value;

            _logger = logger;
        }

        #endregion


        #region SendBidNotificationAsync

        public async Task SendBidNotificationAsync(
            BidNotificationSummaryDto summary,
            int minimumRecordCount)
        {
            try
            {
                // ==========================================
                // VALIDATE SETTINGS
                // ==========================================

                ValidateSettings();


                // ==========================================
                // BUILD HTML
                // ==========================================

                var html =
                    BuildEmailHtml(summary);


                // ==========================================
                // CREATE MESSAGE
                // ==========================================

                var message =
                    new MimeMessage();


                // ==========================================
                // FROM
                // ==========================================

                message.From.Add(
                    new MailboxAddress(
                        _settings.SenderName,
                        _settings.SenderEmail
                    )
                );


                // ==========================================
                // TO
                // ==========================================

                foreach (
                    var receiver
                    in _settings.ReceiverEmails)
                {
                    if (
                        !string.IsNullOrWhiteSpace(
                            receiver))
                    {
                        message.To.Add(
                            MailboxAddress.Parse(
                                receiver.Trim()
                            )
                        );
                    }
                }


                // ==========================================
                // SUBJECT
                // ==========================================

                if (summary.NewRecordCount == 0)
                {
                    message.Subject =
                        "GeM Bid Alert - No New Bids";
                }
                else
                {
                    message.Subject =
                        $"{summary.NewRecordCount} New GeM Bids Added";
                }


                // ==========================================
                // BODY
                // ==========================================

                var bodyBuilder =
                    new BodyBuilder();


                // Plain text version
                if (summary.NewRecordCount == 0)
                {
                    bodyBuilder.TextBody =
                        "GeM Bid Alert\n\n" +
                        "No new GeM bids were added " +
                        "since the previous scheduled notification.\n\n" +
                        $"Total records: {summary.TotalRecordCount}";
                }
                else
                {
                    bodyBuilder.TextBody =
                        $"{summary.NewRecordCount} new GeM bid " +
                        "records have been added.\n\n" +
                        $"Total records: {summary.TotalRecordCount}";
                }


                // HTML version
                bodyBuilder.HtmlBody =
                    html;


                message.Body =
                    bodyBuilder.ToMessageBody();


                // ==========================================
                // SMTP CLIENT
                // ==========================================

                using var smtp =
                    new SmtpClient();


                _logger.LogInformation(
                    "Connecting to SMTP {Server}:{Port}",
                    _settings.SmtpServer,
                    _settings.Port
                );


                // ==========================================
                // CONNECT
                // ==========================================

                await smtp.ConnectAsync(
                    _settings.SmtpServer,
                    _settings.Port,
                    SecureSocketOptions.StartTls
                );


                _logger.LogInformation(
                    "SMTP connection successful."
                );


                // ==========================================
                // AUTHENTICATE
                // ==========================================

                await smtp.AuthenticateAsync(
                    _settings.SenderEmail,
                    _settings.Password
                );


                _logger.LogInformation(
                    "SMTP authentication successful."
                );


                // ==========================================
                // SEND
                // ==========================================

                await smtp.SendAsync(
                    message
                );


                _logger.LogInformation(
                    "GeM bid email sent successfully " +
                    "to {Count} recipients.",
                    _settings.ReceiverEmails.Count
                );


                // ==========================================
                // DISCONNECT
                // ==========================================

                await smtp.DisconnectAsync(
                    true
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

        #endregion


        #region ValidateSettings

        private void ValidateSettings()
        {
            if (
                string.IsNullOrWhiteSpace(
                    _settings.SenderEmail))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SenderEmail is not configured."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    _settings.SenderName))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SenderName is not configured."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    _settings.SmtpServer))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SmtpServer is not configured."
                );
            }


            if (
                _settings.Port <= 0)
            {
                throw new InvalidOperationException(
                    "EmailSettings:Port is not configured."
                );
            }


            if (
                string.IsNullOrWhiteSpace(
                    _settings.Password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:Password is not configured."
                );
            }


            if (
                _settings.ReceiverEmails == null ||
                _settings.ReceiverEmails.Count == 0)
            {
                throw new InvalidOperationException(
                    "No receiver emails configured."
                );
            }
        }

        #endregion


        #region BuildEmailHtml

        private string BuildEmailHtml(
            BidNotificationSummaryDto summary)
        {
            var html =
                new StringBuilder();


            html.Append(
                BuildHeaderAndOpeningSection()
            );


            html.Append(
                BuildSummarySection(summary)
            );


            html.Append(
                BuildCategoryTableHeader()
            );


            html.Append(
                BuildCategoryRows(summary)
            );


            html.Append(
                BuildFooterSection()
            );


            return html.ToString();
        }

        #endregion


        #region Header

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

                    * {
                        box-sizing: border-box;
                    }

                    html,
                    body {
                        margin: 0;
                        padding: 0;
                        width: 100%;
                    }

                    body {
                        background-color: #f3f4f6;
                        font-family: Arial,
                                     Helvetica,
                                     sans-serif;
                        color: #1f2937;
                    }

                    .email-wrapper {
                        width: 100%;
                        padding: 25px 10px;
                    }

                    .email-container {
                        width: 100%;
                        max-width: 750px;
                        margin: 0 auto;
                        background-color: #ffffff;
                        border-radius: 12px;
                        overflow: hidden;
                    }

                    .email-header {
                        background-color: #1d4ed8;
                        color: #ffffff;
                        padding: 25px 20px;
                        text-align: center;
                    }

                    .email-header h1 {
                        margin: 0;
                        font-size: 26px;
                        line-height: 1.3;
                    }

                    .email-header p {
                        margin: 8px 0 0;
                        font-size: 15px;
                        line-height: 1.5;
                    }

                    .email-body {
                        padding: 25px;
                    }

                    .summary-table {
                        width: 100%;
                        border-collapse: collapse;
                    }

                    .summary-table td {
                        padding: 12px;
                        border: 1px solid #d1d5db;
                        font-size: 14px;
                    }

                    .category-wrapper {
                        width: 100%;
                        overflow-x: auto;
                        -webkit-overflow-scrolling: touch;
                    }

                    .category-table {
                        width: 100%;
                        min-width: 500px;
                        border-collapse: collapse;
                        table-layout: fixed;
                    }

                    .category-table th,
                    .category-table td {
                        padding: 11px 8px;
                        border: 1px solid #d1d5db;
                        word-break: break-word;
                        overflow-wrap: anywhere;
                        font-size: 14px;
                    }

                    .category-table th {
                        background-color: #1d4ed8;
                        color: #ffffff;
                        text-align: left;
                    }

                    .footer {
                        padding: 15px;
                        text-align: center;
                        background-color: #f9fafb;
                        color: #6b7280;
                        font-size: 12px;
                        line-height: 1.5;
                    }


                    /* =====================================
                       MOBILE
                       ===================================== */

                    @media only screen and (max-width: 600px)
                    {

                        .email-wrapper {
                            padding: 5px !important;
                        }

                        .email-container {
                            width: 100% !important;
                            border-radius: 6px !important;
                        }

                        .email-header {
                            padding: 20px 12px !important;
                        }

                        .email-header h1 {
                            font-size: 21px !important;
                        }

                        .email-header p {
                            font-size: 13px !important;
                        }

                        .email-body {
                            padding: 15px 10px !important;
                        }

                        .email-body p {
                            font-size: 14px !important;
                        }

                        .summary-table td {
                            padding: 9px 7px !important;
                            font-size: 12px !important;
                        }


                        /* CATEGORY TABLE */

                        .category-wrapper {
                            width: 100% !important;
                            overflow-x: visible !important;
                        }

                        .category-table {
                            min-width: 0 !important;
                            width: 100% !important;
                            table-layout: auto !important;
                        }

                        .category-table thead {
                            display: none !important;
                        }

                        .category-table,
                        .category-table tbody,
                        .category-table tr,
                        .category-table td {
                            display: block !important;
                            width: 100% !important;
                        }

                        .category-table tr {
                            margin-bottom: 10px !important;
                            border: 1px solid #d1d5db !important;
                            border-radius: 6px !important;
                            overflow: hidden !important;
                        }

                        .category-table td {
                            text-align: left !important;
                            border: none !important;
                            border-bottom: 1px solid #e5e7eb !important;
                            padding: 8px 10px !important;
                            font-size: 12px !important;
                        }

                        .category-table td:last-child {
                            border-bottom: none !important;
                        }

                        .category-table td::before {
                            content: attr(data-label);
                            display: block;
                            font-weight: bold;
                            font-size: 10px;
                            text-transform: uppercase;
                            color: #1d4ed8;
                            margin-bottom: 2px;
                        }

                        .footer {
                            padding: 12px !important;
                            font-size: 10px !important;
                        }
                    }

                </style>

            </head>


            <body>

                <div class="email-wrapper">

                    <div class="email-container">


                        <!-- HEADER -->

                        <div class="email-header">

                            <h1>
                                GeM Bid Alert
                            </h1>

                            <p>
                                Scheduled bid notification
                            </p>

                        </div>


                        <!-- BODY -->

                        <div class="email-body">

                            <p style="
                                margin:0 0 15px;
                                font-size:16px;
                                line-height:1.6;
                            ">

                                Hello,

                            </p>
            """;
        }

        #endregion


        #region Summary

        private static string BuildSummarySection(
            BidNotificationSummaryDto summary)
        {
            string message;

            if (summary.NewRecordCount == 0)
            {
                message =
                    "No new GeM bids were added " +
                    "since the previous scheduled notification.";
            }
            else
            {
                message =
                    $"{summary.NewRecordCount} new GeM bid " +
                    "records have been added.";
            }


            return $"""
                    <p style="
                        margin:0 0 20px;
                        font-size:16px;
                        line-height:1.6;
                    ">

                        <strong>
                            {message}
                        </strong>

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
                           border="0">

                        <tbody>

                            <tr>

                                <td style="
                                    background-color:#eff6ff;
                                    font-weight:bold;
                                ">

                                    New records added

                                </td>


                                <td style="
                                    color:#15803d;
                                    font-weight:bold;
                                    text-align:right;
                                ">

                                    {summary.NewRecordCount}

                                </td>

                            </tr>


                            <tr>

                                <td style="
                                    background-color:#eff6ff;
                                    font-weight:bold;
                                ">

                                    Total records

                                </td>


                                <td style="
                                    text-align:right;
                                ">

                                    {summary.TotalRecordCount}

                                </td>

                            </tr>

                        </tbody>

                    </table>
            """;
        }

        #endregion


        #region Category Header

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


                    <div class="category-wrapper">

                        <table class="category-table"
                               width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0">

                            <thead>

                                <tr>

                                    <th style="
                                        width:35%;
                                    ">

                                        Category

                                    </th>


                                    <th style="
                                        width:45%;
                                    ">

                                        Subcategory

                                    </th>


                                    <th style="
                                        width:20%;
                                        text-align:center;
                                    ">

                                        Count

                                    </th>

                                </tr>

                            </thead>


                            <tbody>
            """;
        }

        #endregion


        #region Category Rows

        private static string BuildCategoryRows(
            BidNotificationSummaryDto summary)
        {
            var rows =
                new StringBuilder();


            // ==========================================
            // NO NEW BIDS
            // ==========================================

            if (
                summary.CategoryCounts == null ||
                summary.CategoryCounts.Count == 0)
            {
                rows.Append("""
                    <tr>

                        <td colspan="3"
                            data-label="Status"
                            style="
                                text-align:center;
                                padding:15px;
                                color:#6b7280;
                            ">

                            No new bids in this scheduled notification.

                        </td>

                    </tr>
                    """);

                return rows.ToString();
            }


            // ==========================================
            // CATEGORY ROWS
            // ==========================================

            foreach (
                var category
                in summary.CategoryCounts)
            {
                var categoryKey =
                    HtmlEncoder.Default.Encode(
                        category.CategoryKey ??
                        string.Empty
                    );


                var categorySubKey =
                    HtmlEncoder.Default.Encode(
                        category.CategorySubKey ??
                        string.Empty
                    );


                rows.Append($"""
                    <tr>

                        <td data-label="Category">

                            {categoryKey}

                        </td>


                        <td data-label="Subcategory">

                            {categorySubKey}

                        </td>


                        <td data-label="Count"
                            style="
                                text-align:center;
                                font-weight:bold;
                            ">

                            {category.Count}

                        </td>

                    </tr>
                    """);
            }


            return rows.ToString();
        }

        #endregion


        #region Footer

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

                        <strong>
                            GeM Bid Alert System
                        </strong>

                    </p>


                    </div>


                    <!-- FOOTER -->

                    <div class="footer">

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