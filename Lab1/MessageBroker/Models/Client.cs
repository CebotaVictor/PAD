using PR_c_.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Models
{
    public class Client
    {
        public Socket? socket { get; set; }
        public bool isConnected { get; set; }
        public ClientRole clientRole { get; set; }
        public string? TopicName { get; set; }
    }
}
