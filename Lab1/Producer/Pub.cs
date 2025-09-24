using PR_c_.Enums;
using PR_c_.Helpers;
using PR_c_.Models;
using System.Net;
using System.Net.Sockets;

namespace Client
{

    internal class Pub
    {
        public readonly static string serverIp = "127.0.0.1";
        public readonly static int serverPort = 9000;


        public static async Task SendRole(Socket client)
        {
            await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
            Console.WriteLine("Connected to server {0}, {1}", serverIp, serverPort);

            ClientRole role = ClientRole.Publisher;

            int bytesSend = await Helper.SendData(client, new { role = role});

            if (bytesSend == 0) { Console.WriteLine("Client disconnected"); client.Close(); }
        }

        static async Task Main(string[] args)
        {
            //buffer for incoming data;
            byte[] buffer = new byte[1024];

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await SendRole(client);


            try
            {
                while (true)
                {

                    Console.WriteLine("From which topic do you want to send");

                    string value = Console.ReadLine() ?? "";

                    if (string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine("Enter a valid name");
                        continue;
                    }

                    Console.WriteLine("Write a message");
                    string message = Console.ReadLine() ?? "";

                    if(string.IsNullOrEmpty(message)) { Console.WriteLine("Enter a valid message"); continue; }

                    PacketFrame msgResult = new PacketFrame {
                        TopicName = value,
                        MessageContent = message
                    };

                    int bytesSend = await Helper.SendData(client, msgResult);

                    if (bytesSend == 0) { Console.WriteLine("Client disconnected"); return; }
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket exception: " + ex.Message);
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                client.Close();
            }

        }

    }
}
