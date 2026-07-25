using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Models.Entity;

public partial class BidNotificationState
{
    [Key]
    public int Id { get; set; }

    public int LastProcessedBidId { get; set; }

    public DateTime LastCheckedAt { get; set; }
}
