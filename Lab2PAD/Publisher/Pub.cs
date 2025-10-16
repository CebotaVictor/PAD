using Grpc.Net.Client;
using Lab2;

namespace Publisher
{
    internal class Pub
    {
        static async Task Main(string[] args)
        {            
            using var channel = GrpcChannel.ForAddress("https://localhost:7182");

            var client = new BrokerService.BrokerServiceClient(channel);

            Console.WriteLine("Connected to gRPC broker at 127.0.0.1:9000 as Publisher");

            while (true)
            {
                Console.Write("Enter topic name: ");
                string topic = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(topic))
                {
                    Console.WriteLine("Invalid topic name");
                    continue;
                }

                Console.Write("Enter message: ");
                string message = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(message))
                {
                    Console.WriteLine("Invalid message");
                    continue;
                }

                var packet = new PacketFrameMessage
                {
                    TopicName = topic,
                    MessageContent = message
                };

                try
                {
                    var response = await client.PublishMessageAsync(packet).ResponseAsync;


                    if (response.Success)
                        Console.WriteLine($"Message sent to topic '{topic}'");
                    else
                        Console.WriteLine($"Failed to send message to topic '{topic}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            }
        }
    }
}
