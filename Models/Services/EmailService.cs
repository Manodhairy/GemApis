using GemApi.DTOs.Response;
using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;

namespace GemApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(
            IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendBidNotificationAsync(
            BidNotificationSummaryDto summary,
            int minimumRecordCount)
        {
            var html = new StringBuilder();

            html.Append("""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
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

                        <div style="padding:25px;">

                            <p style="
                                margin-top:0;
                                font-size:16px;">
                                Hello,
                            </p>
                """);

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

                        <tr>
                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;
                                background:#eff6ff;
                                font-weight:bold;">
                                Minimum notification count
                            </td>

                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;">
                                {minimumRecordCount}
                            </td>
                        </tr>

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

                        <tr>
                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;
                                background:#eff6ff;
                                font-weight:bold;">
                                CreatedOn From
                            </td>

                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;">
                                {summary.CreatedOnFrom:dd-MM-yyyy hh:mm tt}
                            </td>
                        </tr>

                        <tr>
                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;
                                background:#eff6ff;
                                font-weight:bold;">
                                CreatedOn To
                            </td>

                            <td style="
                                padding:12px;
                                border:1px solid #d1d5db;">
                                {summary.CreatedOnTo:dd-MM-yyyy hh:mm tt}
                            </td>
                        </tr>
                    </table>
                """);

            // CreatedOn date-wise table
            html.Append("""
                    <h2 style="
                        margin-top:30px;
                        font-size:19px;
                        color:#1d4ed8;">
                        CreatedOn Date-wise Count
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
                                    Date
                                </th>

                                <th style="
                                    padding:12px;
                                    border:1px solid #d1d5db;
                                    text-align:center;">
                                    Record Count
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                """);

            foreach (var item in summary.CreatedDateCounts)
            {
                html.Append($"""
                    <tr>
                        <td style="
                            padding:11px;
                            border:1px solid #d1d5db;">
                            {item.Date:dd-MM-yyyy}
                        </td>

                        <td style="
                            padding:11px;
                            border:1px solid #d1d5db;
                            text-align:center;
                            font-weight:bold;">
                            {item.Count}
                        </td>
                    </tr>
                """);
            }

            html.Append("""
                        </tbody>
                    </table>
                """);

            // Category table
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

            foreach (var category in summary.CategoryCounts)
            {
                string categoryKey = HtmlEncoder.Default.Encode(
                    category.CategoryKey
                );

                string categorySubKey = HtmlEncoder.Default.Encode(
                    category.CategorySubKey
                );

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

            html.Append("""
                                </tbody>
                            </table>

                            <p style="
                                margin-top:30px;
                                margin-bottom:0;
                                color:#4b5563;">
                                Thank you,<br>
                                <strong>GeM Bid Alert System</strong>
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

            using var message = new MailMessage();

            message.From = new MailAddress(
                _settings.SenderEmail.Trim(),
                _settings.SenderName
            );

            message.To.Add(
                _settings.ReceiverEmail.Trim()
            );

            message.Subject =
                $"{summary.NewRecordCount} new GeM bids added";

            message.Body = html.ToString();

            // Important for table formatting
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(
                _settings.Host,
                _settings.Port
            );

            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;

            smtpClient.Credentials =
                new NetworkCredential(
                    _settings.SenderEmail.Trim(),
                    _settings.Password.Trim()
                );

            await smtpClient.SendMailAsync(message);
        }
    }
}