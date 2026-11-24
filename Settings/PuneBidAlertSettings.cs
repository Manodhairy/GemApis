namespace GemApi.Settings;

public class PuneBidAlertSettings
{
    public int CheckIntervalMinutes { get; set; } = 5;

    // Daily morning summary of the previous day's bid alerts.
    public bool DailySummaryEnabled { get; set; } = true;

    // Local server time (HH:mm) after which yesterday's summary is sent.
    public string DailySummaryTime { get; set; } = "08:00";

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string ReplyToEmail { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public List<string> ReceiverEmails { get; set; } = new();
}