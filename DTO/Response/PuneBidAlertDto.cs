namespace GemApi.DTOs.Response;

public class PuneBidAlertDto
{
    public string BidNumber { get; set; } = string.Empty;

    public string? CardStartDate { get; set; }

    public string? CardEndDate { get; set; }

    public string? CategoryKey { get; set; }

    public string? OfficeName { get; set; }

    public string? OrganisationName { get; set; }

    public string? ConsigneeName { get; set; }
}