using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using PR_c_.Models;
using PR_c_.Enums;
using System.Text.Json;
using PR_c_.Helpers;
using System.Text.Json.Serialization;


namespace PR_c_
{
    internal class messageBroker
    {        
        public static bool isRunning = true;
        public static int client_connected = 0;
        // Store messages per topic
        public static ConcurrentDictionary<string, List<PacketFrame>> _packetFrames = new();
        // Store subscribers per topic
        public static ConcurrentDictionary<string, Dictionary<string, Client>> dicSubscribers = new();
        // Store offsets per subscriber per topic
        public static ConcurrentDictionary<string, Dictionary<string, int?>> _subscriberOffsets= new();
        public static List <Client> clients = new();
        public static List <Topic>? topics;
        public static void Main(string[] args)
        {
            InitializeServer();
        }

        public static void InitializeServer()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 9000);
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

            //Console.WriteLine("Connected with {0}, and {1}", clientEndpoint.Address, clientEndpoint.Port);
            
            try
            {

                int bytesRead;
                (string roleMessage, bytesRead) = await Helper.ReceiveData(client.socket!);
                if (bytesRead == 0) { Console.WriteLine("Client disconnected"); return; }
                ClientRole role = ParseRole(roleMessage);
                Console.WriteLine($"Client {clientEndpoint.Address}:{clientEndpoint.Port} identified as {role}");
                
                client.clientRole = role;  


                if(client.clientRole == ClientRole.Subscriber)
                {
                    while (true)
                    {
                        (string subscription, bytesRead) = await Helper.ReceiveData(client.socket!);
                        await Helper.SendBasicData(client.socket!,"hello");
                        
                        var jsonToSubscription = JsonSerializer.Deserialize<Subscription>(subscription);
                        if (jsonToSubscription is not Subscription || string.IsNullOrEmpty(jsonToSubscription.SubscriberName)) { Console.WriteLine("Send a valid subscription frame"); client.socket!.Close(); return; }
                        Console.WriteLine($"Received subscription: {jsonToSubscription.SubscriberName} | {jsonToSubscription.TopicName} ");

                        if (jsonToSubscription != null)
                        {
                            if (!dicSubscribers.ContainsKey(jsonToSubscription.TopicName!))
                            {
                                dicSubscribers[jsonToSubscription.TopicName!] = new Dictionary<string, Client>();
                                if (_packetFrames![jsonToSubscription.TopicName!].Count == 0)
                                    _packetFrames![jsonToSubscription.TopicName!] = new List<PacketFrame>();
                                _subscriberOffsets[jsonToSubscription.TopicName!] = new();
                            }

                            // replace old client if same subscriber reconnects
                            dicSubscribers[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName] = client;

                            // if subscriber offset doesn't exist, set it to -1 (means "no messages received yet")
                            if (!_subscriberOffsets[jsonToSubscription.TopicName!].ContainsKey(jsonToSubscription.SubscriberName))
                            {
                                _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName] = null;
                            }

                            // now send only missed messages
                            var offset = _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName];
                            var messages = _packetFrames[jsonToSubscription.TopicName!];

                            if (messages.Count == 0)
                            {
                                Console.WriteLine($"No messages to send to {jsonToSubscription.SubscriberName} for topic {jsonToSubscription.TopicName}");
                                break;
                            }
                            if (offset != null)
                            {
                                for (int i = (int)offset!; i < messages.Count; i++)
                                {
                                    var jsonData = JsonSerializer.Serialize(messages[i]);
                                    byte[] msgToSend = Encoding.UTF8.GetBytes(jsonData);

                                    try
                                    {
                                        await client.socket!.SendAsync(msgToSend, SocketFlags.None);
                                        Console.WriteLine($"Sent backlog message to {jsonToSubscription.SubscriberName}");
                                        _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName] = i;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to send backlog message: {ex.Message}");
                                        client.isConnected = false;
                                        break;
                                    }
                                }
                                _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName]++;
                            }
                            else if (offset == null)
                            {
                                offset = 0;
                                for (int i = (int)offset; i < messages.Count; i++)
                                {
                                    var jsonData = JsonSerializer.Serialize(messages[i]);
                                    byte[] msgToSend = Encoding.UTF8.GetBytes(jsonData);

                                    try
                                    {
                                        await client.socket!.SendAsync(msgToSend, SocketFlags.None);
                                        Console.WriteLine($"Sent backlog message to {jsonToSubscription.SubscriberName}");
                                        _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName] = i;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to send backlog message: {ex.Message}");
                                        client.isConnected = false;
                                        break;
                                    }
                                }
                                _subscriberOffsets[jsonToSubscription.TopicName!][jsonToSubscription.SubscriberName]++;
                            }

                            ////check if the subscriber already exists by its name and topic name
                            //if (dicSubscribers[jsonToSubscription!.TopicName!].ContainsKey(jsonToSubscription.SubscriberName))
                            //{
                            //    var a = _packetFrames![jsonToSubscription.TopicName!];
                            //    Console.WriteLine($"Subscriber {jsonToSubscription.SubscriberName} already exists for topic {jsonToSubscription.TopicName!}. Sending existing messages.");

                            //    foreach (var msg in a)
                            //    {
                            //        var jsonData = JsonSerializer.Serialize(msg);
                            //        byte[] msgToSend = Encoding.UTF8.GetBytes(jsonData);
                            //        try
                            //        {
                            //            await client.socket!.SendAsync(msgToSend, SocketFlags.None);
                            //            Console.WriteLine($"Message sent to existing subscriber on topic {jsonToSubscription.TopicName!}");
                            //        }
                            //        catch (Exception ex)
                            //        {
                            //            Console.WriteLine($"Failed to send to existing subscriber, removing... {ex.Message}");
                            //            client.isConnected = false;
                            //        }
                            //    }
                            //}
                            //client.TopicName = jsonToSubscription.TopicName;
                            //break;
                        }
                        else
                        {
                            throw new NullReferenceException("Subscription frame is null");
                        }
                        
                        // Check if subscriber already exists for the topic
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
                        client.TopicName = jsonToMessage!.TopicName;
                        Console.WriteLine($"Client {clientEndpoint.Address}, {clientEndpoint.Port} with Topic Message : {data}");
                        if (!_packetFrames!.ContainsKey(jsonToMessage.TopicName!))
                            _packetFrames[jsonToMessage.TopicName!] = new List<PacketFrame>();
                        _packetFrames[jsonToMessage.TopicName!].Add(jsonToMessage);
                        if (dicSubscribers.TryGetValue(jsonToMessage!.TopicName!, out var subs))
                        {
                            Console.WriteLine($"Total number of messages of topic {client.TopicName} is {_packetFrames[jsonToMessage.TopicName!].Count()}");

                            var jsonData = JsonSerializer.Serialize(jsonToMessage);
                            byte[] msg = Encoding.UTF8.GetBytes(jsonData);
                            foreach (var sub in subs)
                            {
                                try
                                {
                                    await sub.Value.socket!.SendAsync(msg, SocketFlags.None);
                                    Console.WriteLine($"Message sent to subscriber on topic {sub.Value.socket!.RemoteEndPoint}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to send to subscriber, removing... {ex.Message}");
                                    sub.Value.isConnected = false;
                                }
                            }
                        }


                    }
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
