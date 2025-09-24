using PR_c_.Enums;
using PR_c_.Helpers;
using PR_c_.Infrastructure;
using PR_c_.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Client
{

    internal class Pub
    {
        public readonly static string serverIp = "127.0.0.1";
        public readonly static int serverPort = 5050;
        private static List<Topic>? topics;


        public static async Task SendRole(Socket client)
        {
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
            Console.WriteLine("Connected to server {0}, {1}", serverIp, serverPort);

            ClientRole role = ClientRole.Publisher;

            var jsonObj = JsonSerializer.Serialize(new{role = role});
            byte[] sentData = Encoding.UTF8.GetBytes(jsonObj);
            await client.SendAsync(sentData);
        }

        static async Task Main(string[] args)
        {
            //buffer for incoming data;
            byte[] buffer = new byte[1024];

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await SendRole(client);
            
            int bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None);
            string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            topics = JsonSerializer.Deserialize<List<Topic>>(receivedData);

            if (topics!.Count == 0) { Console.WriteLine("There are no Topics, first create topics by invoking producers");}

            Topic topic = new();
            while (true)
            {
                foreach (var tp in topics!)
                {
                    Console.WriteLine($"{tp.Name}");
                }
                Console.WriteLine("From which topic do you want to send");

                string value = Console.ReadLine() ?? "";

                if (string.IsNullOrEmpty(value))
                {
                    Console.WriteLine("Enter a valid name");
                    continue;
                }

                topic.Name = value;
                string jsonTopic = JsonSerializer.Serialize(topic);
                int bytesSend = await Helper.SendData(client, jsonTopic);
                break;
            }



                //var exTopic = topics.FirstOrDefault(t => t.Name!.Equals(value, StringComparison.OrdinalIgnoreCase));

                //if (exTopic != null)
                //{
                //    topic.Name = value;
                //    break;
                //}
                //else
                //{
                //    topic.Name = value;
                //    var response = await MBService.AddTopic(topic);
                //    if (response.Status == false) continue;
                //    topics = await MBService.GetAllTopics() ?? new List<Topic>();
                //}
            //}                                                           

            try
            {
                while (true)
                {
                    Console.WriteLine("Write a message");
                    string message = Console.ReadLine() ?? "";

                    if(string.IsNullOrEmpty(message)) { Console.WriteLine("Enter a valid message"); continue; }

                    PacketFrame msgResult = new PacketFrame {
                        TopicName = topic.Name,
                        MessageContent = message
                    };


                    string json = JsonSerializer.Serialize(msgResult);

                    byte[] dataToSend = Encoding.UTF8.GetBytes(json);

                    await client.SendAsync(dataToSend);
                    //if (message.ToLower() == "exit") { break; }
                }

                client.Close();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

        }

    }
}
