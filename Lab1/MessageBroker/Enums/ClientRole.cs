using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PR_c_.Enums
{

    [JsonConverter(typeof(JsonStringEnumConverter<ClientRole>))]
    public enum ClientRole
    {   
        Publisher,
        Subscriber
    }
}
