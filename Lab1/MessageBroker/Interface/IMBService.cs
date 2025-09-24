using PR_c_.EFCore;
using PR_c_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Interface
{
    internal interface IMBService
    {
        Task<ResponseMessage> AddMessage(Message message);
        Task<ResponseMessage> CreateSubscription(int TopicId, Subscription subscription);
        Task<ResponseMessage> AddTopic(Topic Topic);
        //Task<Message> GetSubscriberMessages(ushort Id, Subscription Sub);
        //Task<Subscription> GetSubscriptionById(ushort Id);
        Task<List<Topic>> GetAllTopics();

    }
}
