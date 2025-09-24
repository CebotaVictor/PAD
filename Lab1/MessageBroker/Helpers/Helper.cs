using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace PR_c_.Helpers
{
    public class Helper
    {
        public static async Task<int> SendData<T>(Socket client, T dataToSerialize)
        {
            var jsonT = JsonSerializer.Serialize(dataToSerialize);
            Console.WriteLine(jsonT);
            byte[] dataToSend = Encoding.UTF8.GetBytes(jsonT);
            return await client.SendAsync(dataToSend);
        }

        public static async Task<(string, int receivedBytes)> ReceiveData(Socket Client)
        {
            byte[] buffer = new byte[1024]; 
            int bytesRead = await Client.ReceiveAsync(buffer);
            string result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return (result, bytesRead);
        }
    }
}
