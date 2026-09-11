using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GemApi.Models.Entity;

[Table("PuneBidAlertSent")]
public class PuneBidAlertSent
{
    [Key]
    public int Id { get; set; }

    public int BidId { get; set; }

    public string BidNumber { get; set; } = string.Empty;

    public DateTime? BidUpdatedOn { get; set; }

    public DateTime ChangeDetectedOn { get; set; }

    public DateTime SentOn { get; set; }
}