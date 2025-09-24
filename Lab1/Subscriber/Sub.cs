using PR_c_.Enums;
using PR_c_.Helpers;
using PR_c_.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Subscriber
{
    internal class Sub
    {
        public readonly static string serverIp = "127.0.0.1";
        public readonly static int serverPort = 9000;
        private static List<Topic>? topics;

        public static async Task SendRole(Socket client)
        {
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
            Console.WriteLine("Connected to server {0}, {1}", serverIp, serverPort);

            ClientRole role = ClientRole.Subscriber;

            int bytesSend = await Helper.SendData(client, new { role = role });

            if (bytesSend == 0) { Console.WriteLine("Client disconnected"); client.Close(); }
        }
        static async Task Main(string[] args)
        {
            //send role of the client
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await SendRole(client);

            Subscription subscription = new();
            while (true)
            {
                Console.WriteLine("To what topic do you subscribe : ");

                string? value = Console.ReadLine() ?? "";
                if (string.IsNullOrEmpty(value)) { Console.WriteLine("Input a valid name"); continue; }

                subscription.SubscriberName = $"Sub{value}";
                subscription.TopicName = value;

                int bytesSend = await Helper.SendData(client, subscription);
                break;
            }

            try
            {
                byte[] buffer = new byte[1024];

                while (true)
                {
                    (string response, int bytesReceived) = await Helper.ReceiveData(client);
                    if (bytesReceived == 0) { Console.WriteLine("Connection closed by the server"); break; }
                    Console.WriteLine(response);
                    var jsonToMessage =JsonSerializer.Deserialize<Message>(response);

                    if (jsonToMessage == null) { Console.WriteLine("Message is corrupted try again"); continue; }

                    Console.WriteLine($"New message on topic {jsonToMessage.TopicName} : {jsonToMessage.TopicMessage}");
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
