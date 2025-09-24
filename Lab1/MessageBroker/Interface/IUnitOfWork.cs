using PR_c_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Interface
{
    public interface IUnitOfWork
    {
        public IGenericRepository<Topic> TopicRepository { get; }
        public IGenericRepository<Message> MessageRepository { get; }
        public IGenericRepository<Subscription> SubscriptionRepository { get; }
        Task<int> SaveChangesAsync();
    }
}
