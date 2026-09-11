namespace GemApi.Settings;

public class PuneBidAlertSettings
{
    public int CheckIntervalMinutes { get; set; } = 5;

    public string ApiKey { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public List<string> ReceiverEmails { get; set; } = new();
}