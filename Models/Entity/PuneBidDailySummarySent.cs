using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GemApi.Models.Entity;

[Table("PuneBidDailySummarySent")]
public class PuneBidDailySummarySent
{
    [Key]
    public int Id { get; set; }

    // The day that was reported, e.g. 2026-11-24.
    public DateTime ReportDate { get; set; }

    // When the summary mail was sent.
    public DateTime SentOn { get; set; }
}
