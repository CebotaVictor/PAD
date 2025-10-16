using Grpc.Net.Client;
using Lab2;
namespace Subscriber
{
    internal class Sub
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:7182");

            var client = new BrokerService.BrokerServiceClient(channel);

            Console.WriteLine("Connected to gRPC broker at 127.0.0.1:9000 as Publisher");

            while (true)
            {
                Console.Write("Enter subscriber name: ");
                string name = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(name))
                {
                    Console.WriteLine("Invalid subscriber name");
                    continue;
                }

                Console.Write("Enter topic: ");
                string topic = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(topic))
                {
                    Console.WriteLine("Invalid message");
                    continue;
                }

                var subscribe = new Subscription
                {
                    SubscriberName = name,
                    TopicName = topic
                };

                try
                {
                    using var streamingCall = client.Subscribe(subscribe);

                    var responseStream = streamingCall.ResponseStream;
                    while (await responseStream.MoveNext(CancellationToken.None))
                    {
                        var message = responseStream.Current;
                        Console.WriteLine($"[{message.TopicName}] {message.MessageContent}");
                    }

                    Console.WriteLine("Subscription ended by server.");

                  
                }
                catch (Grpc.Core.RpcException ex)
                {
                    Console.WriteLine($"gRPC error: {ex.Status.Detail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

        }
    }
}
