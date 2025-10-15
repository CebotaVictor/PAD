using Grpc.Core;
using PR_c_.Grpc;
using System.Collections.Concurrent;

namespace PR_c_.Services
{
    public class GrpcBrokerService : BrokerService.BrokerServiceBase
    {
        private static readonly ConcurrentDictionary<string, List<PacketFrame>> _topicMessages = new();
        private static readonly ConcurrentDictionary<string, List<IServerStreamWriter<PacketFrame>>> _topicSubscribers = new();

        private readonly ILogger<GrpcBrokerService> _logger;

        public GrpcBrokerService(ILogger<GrpcBrokerService> logger)
        {
            _logger = logger;
        }

        public override Task<Empty> PublishMessage(PacketFrame request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.TopicName))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "TopicName cannot be empty"));

            _logger.LogInformation($"Publishing message to topic {request.TopicName}: {request.MessageContent}");

            var messageList = _topicMessages.GetOrAdd(request.TopicName, _ => new List<PacketFrame>());
            lock (messageList)
            {
                messageList.Add(request);
            }

            if (_topicSubscribers.TryGetValue(request.TopicName, out var subscribers))
            {
                foreach (var sub in subscribers.ToList())
                {
                    try
                    {
                        sub.WriteAsync(request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Subscriber disconnected: {ex.Message}");
                        subscribers.Remove(sub);
                    }
                }
            }

            return Task.FromResult(new Empty());
        }

        public override async Task Subscribe(Subscription request, IServerStreamWriter<PacketFrame> responseStream, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.TopicName) || string.IsNullOrWhiteSpace(request.SubscriberName))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid subscription"));

            _logger.LogInformation($"Subscriber {request.SubscriberName} subscribed to {request.TopicName}");

            var subscribers = _topicSubscribers.GetOrAdd(request.TopicName, _ => new List<IServerStreamWriter<PacketFrame>>());
            subscribers.Add(responseStream);

            // Send backlog
            if (_topicMessages.TryGetValue(request.TopicName, out var messages))
            {
                foreach (var msg in messages)
                {
                    await responseStream.WriteAsync(msg);
                }
            }

            // Keep stream open until cancelled
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            _logger.LogInformation($"Subscriber {request.SubscriberName} disconnected from {request.TopicName}");
            subscribers.Remove(responseStream);
        }
    }
}
