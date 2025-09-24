using PR_c_.Enums;
using PR_c_.Helpers;
using PR_c_.Infrastructure;
using PR_c_.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Subscriber
{
    internal class Sub
    {
        public readonly static string serverIp = "127.0.0.1";
        public readonly static int serverPort = 5050;
        private static List<Topic>? topics;

        public static async Task SendRole(Socket client)
        {
            ClientRole role = ClientRole.Subscriber;

            var jsonObj = JsonSerializer.Serialize(new { role = role });

            byte[] sentData = Encoding.UTF8.GetBytes(jsonObj);

            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
            Console.WriteLine("Connected to server {0}, {1}", serverIp, serverPort);

            await client.SendAsync(sentData);
        }
        static async Task Main(string[] args)
        {
            //send role of the client
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await SendRole(client);

            (string jsonReceived, int dataReceived) = await Helper.ReceiveData(client);
            if (dataReceived == 0) { Console.WriteLine("Connection closed by the server"); client.Close(); return; }
            var jsonToString = JsonSerializer.Deserialize<List<Topic>>(jsonReceived);
            if(jsonToString == null) { Console.WriteLine("There was an error while fetching topics"); return; }
            topics = jsonToString;
            if (topics.Count == 0){ Console.WriteLine("There are no Topics, first create topics by invoking producers");  return; }
            Subscription subscription = new();
            while (true)
            {
                Console.WriteLine("To what topic do you subscribe(Id) : ");
                foreach (var tp in topics)
                {
                    Console.WriteLine($"{tp.Name}");
                }

                string? value = Console.ReadLine() ?? "";
                if (string.IsNullOrEmpty(value)) { Console.WriteLine("Input a valid name"); continue; }

                subscription.Name = $"Sub{value}";
                subscription.TopicName = value;
                string json = JsonSerializer.Serialize(subscription);
                int bytesSend = await Helper.SendData(client, json);
                break;
                //if (ushort.TryParse(value, out ushort topicId))
                //{

                //subscription.Name = $"Sub{value}";
                //subscription.TopicId = topicId;

                //var response = await MBService.CreateSubscription(topicId, subscription);

                //if (response.Status)
                //{
                //    Console.WriteLine("You are successfully subscribed to the topic");
                //    await client.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(subscription)));
                //    break;
                //}
                //else
                //{
                //    Console.WriteLine("Subscription failed, try again");
                //    return;
                //}
                //}
            }

            try
            {
                byte[] buffer = new byte[1024];

                while (true)
                {
                    int bytesReceived = client.Receive(buffer);
                    if (bytesReceived == 0) { Console.WriteLine("Connection closed by the server"); break; }
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
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
