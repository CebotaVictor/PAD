using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Models
{
    public class Message
    {
        [Key]
        public ushort Id { get; set; }

        [Required]
        public string? TopicMessage { get; set; }

        [Required]
        public string? TopicName { get; set; }

        public string? SubscriptionName { get; set; }

        [Required]
        public DateTime? ExpiresAfter { get; set; } = DateTime.Now.AddMinutes(60);

        [Required]
        public string MessageStatus { get; set; } = "NEW";  
        
    }
}
