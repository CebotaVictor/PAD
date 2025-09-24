using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using PR_c_.Models;
using PR_c_.Infrastructure;
using PR_c_.Enums;
using System.Text.Json;
using System.Security.Cryptography;
using System.Security.AccessControl;
using PR_c_.Helpers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;


namespace PR_c_
{
    internal class messageBroker
    {        
        public static bool isRunning = true;
        public static int client_connected = 0;
        public static Dictionary<string, List<Client>> dicSubscribers = new();
        public static List <Client> clients = new List<Client>();
        public static List <Topic>? topics;
        public static void Main(string[] args)
        {
            InitializeServer();
        }

        public static void InitializeServer()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 5050);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            socket.Listen(10);

            Console.WriteLine($"Waiting for a client, IpAddress {((IPEndPoint)socket.LocalEndPoint!).Address.ToString()} and port {((IPEndPoint)socket.LocalEndPoint!).Port.ToString()}");


            while (isRunning)
            {
                try
                {
                    Socket clientSocket = socket.Accept();
                    if (clientSocket.Connected == true && client_connected < 10)
                    {
                        Console.WriteLine("Connection accepted and the socket is accessible.");
                        //socket
                        Client client = new Client();
                        client.socket = clientSocket;
                        client.isConnected = true;
                        client_connected++;

                        lock (clients)
                        {
                            clients.Add(client);
                        }

                        if (client_connected > 0)
                        {
                            Task.Run(() => HandleClientAsync(client));
                        }
                    }
                    else { Console.WriteLine("Connection accepted but the socket is not accessible."); return; }
                }
                catch (SocketException e)
                {
                    if (e.NativeErrorCode.Equals(10035))
                    {
                        Console.WriteLine("Still Connected, but the Send would block");
                    }
                    else
                    {
                        Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode);
                    }
                }
            }
        }


        private static async void HandleClientAsync(Client client)
        {
            if (client == null) return;
            Socket? socket = client.socket;

            IPEndPoint clientEndpoint = null!;

            if (socket?.RemoteEndPoint is IPEndPoint)
            {
                clientEndpoint = (IPEndPoint)socket.RemoteEndPoint!;
            }

            else { Console.WriteLine("Warning: Client RemoteEndPoint is null. Closing connection");  socket?.Close(); }

            Console.WriteLine("Connected with {0}, and {1}", clientEndpoint.Address, clientEndpoint.Port);

            
            try
            {
                int bytesRead;
                (string roleMessage, bytesRead) = await Helper.ReceiveData(client.socket!);
                if (bytesRead == 0) { Console.WriteLine("Client disconnected"); return; }
                ClientRole role = ParseRole(roleMessage);
                Console.WriteLine($"Client {((IPEndPoint)socket!.LocalEndPoint!).AddressFamily}:{((IPEndPoint)socket.LocalEndPoint!).Port} identified as {role}");
                
                client.clientRole = role;  

                //if (topics.Count == 0) { Console.WriteLine("There are no Topics, first create topics by invoking producers"); return; }

                topics = await MBService.GetAllTopics() ?? new List<Topic>();
                var jsonTopics = JsonSerializer.Serialize(topics);
                await Helper.SendData(client.socket!, jsonTopics);

                if(client.clientRole == ClientRole.Publisher)
                {
                    while (true)
                    {
                        (string topicName, bytesRead) = await Helper.ReceiveData(client.socket!);
                        Console.WriteLine($"Received topic name: {topicName}");
                        var topic = JsonSerializer.Deserialize<Topic>(topicName);
                        if (topic is not Topic || string.IsNullOrEmpty(topic.Name)) { Console.WriteLine("Send a valid topic"); client.socket!.Close(); return;}

                        var exTopic = topics.FirstOrDefault(t => t.Name!.Equals(topic.Name, StringComparison.OrdinalIgnoreCase));

                        if (exTopic == null)
                        {
                            var response = await MBService.AddTopic(topic);
                            if (response.Status == false) continue;
                            topics = await MBService.GetAllTopics() ?? new List<Topic>();
                            break;
                        }
                        break;
                    }
                }

                if(client.clientRole == ClientRole.Subscriber)
                {
                    while (true)
                    {
                        (string subscription, bytesRead) = await Helper.ReceiveData(client.socket!);
                        var jsonToSubscription = JsonSerializer.Deserialize<Subscription>(subscription);
                        if (jsonToSubscription is not Subscription|| string.IsNullOrEmpty(jsonToSubscription.Name)) { Console.WriteLine("Send a valid topic"); client.socket!.Close(); return; }
                        Console.WriteLine($"Received subscription: {jsonToSubscription.Name} | {jsonToSubscription.TopicName} ");

                        var exTopic = topics.FirstOrDefault(t => t.Name!.Equals(jsonToSubscription.TopicName, StringComparison.OrdinalIgnoreCase));
                        if (exTopic == null)
                        {
                            Console.WriteLine("Topic does not exist, send a valid topic");
                            client.socket!.Close();
                            break;
                        }

                        if (jsonToSubscription != null)
                        {
                            lock (dicSubscribers)
                            {
                                if (!dicSubscribers.ContainsKey(jsonToSubscription.TopicName!))
                                    dicSubscribers[jsonToSubscription.TopicName!] = new List<Client>();

                                var subClient = clients.First(c => c.socket == client.socket);
                                subClient.TopicName = jsonToSubscription.TopicName;
                                dicSubscribers[jsonToSubscription.TopicName!].Add(subClient);
                            }
                            Console.WriteLine($"Subscriber added to topic {jsonToSubscription.TopicName!}");
                        }
                        break;
                    }
                }



                while (true)
                {
                    byte[] buffer = new byte[1024];
                    
                    (string data, bytesRead) = await Helper.ReceiveData(client.socket!);
                    if (bytesRead == 0) { Console.WriteLine("Client disconnected"); break; }

                    if (role == ClientRole.Publisher)
                    {
                        var jsonToMessage = JsonSerializer.Deserialize<PacketFrame>(data);
                        if (jsonToMessage == null) Console.WriteLine("Message is corrupted try again");

                        Console.WriteLine($"Client {clientEndpoint.Address}, {clientEndpoint.Port} Message : {data}");

                        //var subs = MBService.GetAllSubscribersByTopicId(jsonToMessage!.TopicId);
                        //if (subs.Count == 0)
                        //{
                        //    Console.WriteLine("No subscribers for this topic");
                        //    client.Close();
                        //}

                        if (dicSubscribers.TryGetValue(jsonToMessage!.TopicName!, out var subs))
                        {
                            Message message = new Message
                            {
                                TopicName = jsonToMessage.TopicName,
                                TopicMessage = jsonToMessage.MessageContent,
                            };
                            var res = await MBService.AddMessage(message);

                            var jsonData = JsonSerializer.Serialize(jsonToMessage);
                            byte[] msg = Encoding.UTF8.GetBytes(jsonData);
                            foreach (var sub in subs)
                            {
                                try
                                {
                                    await sub.socket!.SendAsync(msg, SocketFlags.None);
                                    Console.WriteLine($"Message sent to subscriber on topic {sub.socket.RemoteEndPoint}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to send to subscriber, removing... {ex.Message}");
                                    sub.isConnected = false;
                                }
                            }
                        }
                    }

                    //else if (role == ClientRole.Subscriber)
                    //{
                    //    var subscription = JsonSerializer.Deserialize<Subscription>(data);
                    //    if (subscription != null)
                    //    {
                    //        lock (dicSubscribers)
                    //        {
                    //            if (!dicSubscribers.ContainsKey(subscription.TopicName!))
                    //                dicSubscribers[subscription.TopicName!] = new List<Client>();

                    //            var subClient = clients.First(c => c.socket == client.socket);
                    //            subClient.TopicName = subscription.TopicName;
                    //            dicSubscribers[subscription.TopicName!].Add(subClient);
                    //        }
                    //        Console.WriteLine($"Subscriber added to topic {subscription.TopicName!}");
                    //    }
                    //}
                }


            }
            catch (SocketException e)
            {
                Console.WriteLine($"Client disconnected with error {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred {e}");
            }

            Console.WriteLine("Disconnected from {0}", clientEndpoint.Address);
            client.socket!.Close();
        }

        private static ClientRole ParseRole(string message)
        {
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
            var json = JsonSerializer.Deserialize<Role>(message, options);

            ClientRole jsonRole = json!.role == ClientRole.Publisher ? ClientRole.Publisher : json.role;
            return jsonRole;
        }
    }
}
