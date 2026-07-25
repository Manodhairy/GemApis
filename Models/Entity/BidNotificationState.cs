using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GemApi.Models.Entity
{
    [Table("BidNotificationStates")]
    public class BidNotificationState
    {
        [Key]
        public int Id { get; set; }

        public int LastProcessedBidId { get; set; }

        public DateTime LastCheckedAt { get; set; }
    }
}