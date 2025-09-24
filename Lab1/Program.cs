using PR_c_.Enums;
using PR_c_.Infrastructure;
using PR_c_.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{

    internal class Program
    {
        public readonly static string serverIp = "127.0.0.1";
        public readonly static int serverPort = 5050;
        private static List<Topic>? topics;


        public static async Task SendRole(Socket client)
        {
            ClientRole role = ClientRole.Publisher;

            var jsonObj = System.Text.Json.JsonSerializer.Serialize(role);

            byte[] sentData = Encoding.UTF8.GetBytes(jsonObj);

            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
            Console.WriteLine("Connected to server {0}, {1}", serverIp, serverPort);

            await client.SendAsync(sentData);
        }
        static async Task Main(string[] args)
        {
            //buffer for incoming data;
            byte[] buffer = new byte[1024];

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await SendRole(client);

            topics = await MBService.GetAllTopics() ?? new List<Topic>();
            if (topics.Count == 0) { Console.WriteLine("There are no Topics, first create topics by invoking producers"); return; }
            Topic topic = new();

            while (true) {

                foreach (var tp in topics)
                {
                    Console.WriteLine($"{tp.Id}|{tp.Name}");
                }
                Console.WriteLine("From which topic do you want to send");

                string value = Console.ReadLine() ?? "";

                if (string.IsNullOrEmpty(value))
                {
                    Console.WriteLine("Enter a valid name");
                    continue;
                }
                var exTopic = topics.FirstOrDefault(t => t.Name!.Equals(value, StringComparison.OrdinalIgnoreCase));

                if(exTopic != null)
                {
                    topic.Name = value;
                    topic.Id = exTopic.Id;
                    break;
                }
                else
                {
                    topic.Name = value;
                    var response = await MBService.AddTopic(topic);
                    if (response.Status == false) continue;
                    topics = await MBService.GetAllTopics() ?? new List<Topic>();
                }
            }

            try
            {
                while (true)
                {
                    Console.WriteLine("Write a message");
                    string message = Console.ReadLine() ?? "";

                    SendData msgResult = new();

                    if (topic.Id == null)
                    {
                        msgResult.TopicId = (ushort)topics.FirstOrDefault(t => t.Name!.Equals(topic.Name, StringComparison.OrdinalIgnoreCase))!.Id!;
                        msgResult.TopicMessage = message;
                    }
                    else
                    {
                        msgResult.TopicId = (ushort)topic.Id;
                        msgResult.TopicMessage = message;
                    }

                    string json = System.Text.Json.JsonSerializer.Serialize(msgResult);
                        
                    byte[] dataToSend = Encoding.UTF8.GetBytes(json);

                    client.SendAsync(dataToSend);
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
