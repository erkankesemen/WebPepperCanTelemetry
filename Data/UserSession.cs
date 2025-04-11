using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebPepperCan.Data
{
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
} 