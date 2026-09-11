using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GemApi.Models.Entity;

[Table("PuneBidAlertState")]
public class PuneBidAlertState
{
    [Key]
    public int Id { get; set; }

    public DateTime LastCheckedAt { get; set; }
}