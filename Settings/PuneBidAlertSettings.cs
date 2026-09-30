namespace GemApi.Settings;

public class PuneBidAlertSettings
{
    public int CheckIntervalMinutes { get; set; } = 5;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = "smtp.office365.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public List<string> ReceiverEmails { get; set; } = new();
}