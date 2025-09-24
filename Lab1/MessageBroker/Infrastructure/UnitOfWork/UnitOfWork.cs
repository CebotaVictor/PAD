using PR_c_.EFCore;
using PR_c_.Interface;
using PR_c_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Infrastructure.UnitOfWork
{
    internal class UnitOfWork : IUnitOfWork
    {
        public UnitOfWork(IGenericRepository<Topic> topicRepository, IGenericRepository<Message> messageRepository, IGenericRepository<Subscription> subscriptionRepository, MBDbContext context)
        {
            _context = context ?? throw new ArgumentNullException("MBContext is null");
            TopicRepository = topicRepository ?? throw new ArgumentNullException("topicRepository in null");
            MessageRepository = messageRepository ?? throw new ArgumentNullException("messageRepository is null");
            SubscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException("subscriptionRepository is null");
        }

        private readonly MBDbContext _context;
        public IGenericRepository<Topic> TopicRepository { get; }
        public IGenericRepository<Message> MessageRepository { get; }
        public IGenericRepository<Subscription> SubscriptionRepository { get; }


        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
