using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Models
{
    public record class PacketFrame
    {
        public string? TopicName { get; set; }
        public String? MessageContent { get; set; }
    }
}
