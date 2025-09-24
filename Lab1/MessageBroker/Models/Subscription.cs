using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Models
{
    public class Subscription
    {
        [Key]
        public string? SubscriberName { get; set; }

        [Required]
        public string? TopicName { get; set; }
    }
}
