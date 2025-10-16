using Grpc.Core;
//using PR_c_.Models; // Assuming Client, ClientRole, Subscription, PacketFrame are defined here
using System.Collections.Concurrent;
// Using the generated namespace from the .proto file
using Lab2;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
//using PR_c_.Grpc;

namespace Lab2.Services
{
    // Inherit from the generated base class
    public class GRPCBrokerService : BrokerService.BrokerServiceBase
    {
        private readonly ILogger<GRPCBrokerService> _logger;

        public GRPCBrokerService(ILogger<GRPCBrokerService> logger)
        {
            _logger = logger;
        }

        private static readonly ConcurrentDictionary<string, List<PacketFrameMessage>> _topicMessages = new();
        private static readonly ConcurrentDictionary<string, List<IServerStreamWriter<PacketFrameMessage>>> _topicSubscribers = new();
        public static ConcurrentDictionary<string, Dictionary<string, int?>> _subscriberOffsets = new();

        public override Task<ResponseMessage> PublishMessage(PacketFrameMessage request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TopicName))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "TopicName cannot be empty"));

                _logger.LogInformation($"Publishing message to topic {request.TopicName}: {request.MessageContent}");

                var messageList = _topicMessages.GetOrAdd(request.TopicName, _ => new List<PacketFrameMessage>());
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
                return Task.FromResult(new ResponseMessage {Success = true});
            }
            catch(Exception ex) {
                _logger.LogError(ex.Message);
                return Task.FromResult(new ResponseMessage(new ResponseMessage { Success = false }));
            }
        }

        public override async Task Subscribe(Subscription request, IServerStreamWriter<PacketFrameMessage> responseStream, ServerCallContext context)
        {
            List<IServerStreamWriter<PacketFrameMessage>> subscribers = new List<IServerStreamWriter<PacketFrameMessage>>();
            try
            {
                if (string.IsNullOrWhiteSpace(request.TopicName) || string.IsNullOrWhiteSpace(request.SubscriberName))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid subscription"));

                _logger.LogInformation($"Subscriber {request.SubscriberName} subscribed to {request.TopicName}");

                subscribers = _topicSubscribers.GetOrAdd(request.TopicName, _ => new List<IServerStreamWriter<PacketFrameMessage>>());
                subscribers.Add(responseStream);
                subscribers = _topicSubscribers.GetOrAdd(request.TopicName, _ => new List<IServerStreamWriter<PacketFrameMessage>>());
                var topicOffsets = _subscriberOffsets.GetOrAdd(request.TopicName, _ => new Dictionary<string, int?>());

                if (!topicOffsets.ContainsKey(request.SubscriberName))
                    topicOffsets[request.SubscriberName] = null;

                var offset = _subscriberOffsets[request.TopicName!][request.SubscriberName];
                var messagesList = _topicMessages.GetOrAdd(request.TopicName, _ => new List<PacketFrameMessage>());
                int startIndex = topicOffsets[request.SubscriberName] ?? 0;



                for (int i = startIndex; i < messagesList.Count; i++)
                {
                    try
                    {
                        await responseStream.WriteAsync(messagesList[i]);
                        topicOffsets[request.SubscriberName] = i + 1; 
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to send backlog message to {request.SubscriberName}: {ex.Message}");
                        break; 
                    }
                }

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                _logger.LogInformation($"Subscriber {request.SubscriberName} disconnected from {request.TopicName}");
                subscribers.Remove(responseStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                subscribers.Remove(responseStream);
                throw new RpcException(new Status(StatusCode.Unknown, "Disconnected due to server error"));
            }
        }

    }

}