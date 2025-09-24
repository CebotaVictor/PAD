using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using PR_c_.EFCore;
using PR_c_.Infrastructure.UnitOfWork;
using PR_c_.Interface;
using PR_c_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.Infrastructure
{
    public class MBService
    {


        //Add Topic
        public static async Task<ResponseMessage> AddTopic(Topic Topic)
        {
            try
            {
                if (Topic == null) throw new ArgumentNullException("Topic is null in MBRepo");

                using (var context = new MBDbContext())
                {                    
                    await context.Topics.AddAsync(Topic);
                    await context.SaveChangesAsync();
                }
                return new ResponseMessage { Status = true };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while adding the topic: {ex.Message}");
                return new ResponseMessage { Status = false };
            }
        }

        //Add Message
        public static async Task<ResponseMessage> AddMessage(Message message)
        {
            try
            {
                using (var context = new MBDbContext())
                {
                    string TopicName = message.TopicName!;
                    if (!await context.Topics.AnyAsync(t => t.Name!.ToLower() == TopicName.ToLower()))
                    {
                        throw new Exception("Topic id of the message was not found");
                    }

                    IQueryable<Subscription> subs = context.Subscriptions.AsQueryable().Where(s => s.TopicName == message.TopicName);

                    if (subs.Count() == 0)
                    {
                        throw new Exception("There are no subscriptions for this topic");
                    }

                    foreach (var sub in subs)
                    {
                        message.SubscriptionName = sub.SubscriberName!;
                        await context.Messages.AddAsync(message);
                    }
                    await context.SaveChangesAsync();
                }
                return new ResponseMessage { Status = true };
            }
            catch(Exception ex)
            {
                Console.WriteLine($"An error occurred while adding the message: {ex.Message}");
                return new ResponseMessage { Status = false};
            }
        }

        //Add Subscription
        public static async Task<ResponseMessage> CreateSubscription(string TopicName, Subscription subscription)
        {
            try
            {
                if(subscription == null) throw new ArgumentNullException("Subscription is null in MBRepo");


                using (var context = new MBDbContext())
                {
                    if (!await context.Topics.AnyAsync(t => t.Name == TopicName))
                    {
                        throw new Exception("Topic id of the subscription was not found");
                    }

                       
                    subscription.TopicName = TopicName;
                    await context.Subscriptions.AddAsync(subscription);
                    await context.SaveChangesAsync();
                }
                return new ResponseMessage { Status = true };

            }
            catch(Exception ex) 
            {
                Console.WriteLine($"An error occurred while creating the subscription: {ex.Message}");
                return new ResponseMessage { Status = false };
            }
        }



        public static async Task<List<Topic>> GetAllTopics()
        {
            using (var context = new MBDbContext()) {
                return await context.Topics.ToListAsync() ?? new List<Topic>();
            }
        }


        public static async Task<Topic> GetTopicByName(string name)
        {
            using(var context = new MBDbContext())
            {
                var topic = await context?.Topics?.AsQueryable()?.FirstOrDefaultAsync(t => t.Name == name)!;
                if (topic == null) return null!;
                return topic;
            }
        }
        
        public static async Task<List<Subscription>> GetAllSubscribersAsync()
        {
            using (var context = new MBDbContext())
            {
                return await context.Subscriptions.ToListAsync() ?? new List<Subscription>();
            }
        }

        public static List<Subscription> GetAllSubscribersByTopicId(string TopicName)
        {
            using (var context = new MBDbContext())
            {
                return context.Subscriptions.AsQueryable().Where(s => s.SubscriberName == TopicName).ToList() ?? new List<Subscription>();
            } 
        }

        public static async Task<List<Message>> GetSubscriberMessages(string? SubscriberName)
        {
            try
            {
                using (var context = new MBDbContext())
                {
                    if (!await context.Subscriptions.AnyAsync(s => s.SubscriberName == SubscriberName))
                    {
                        throw new Exception("SubscriberId of the subscription was not found");
                    }

                    IQueryable<Message> messages = context.Messages.AsQueryable().Where(m => m.SubscriptionName == SubscriberName && m.MessageStatus != "SENT");
                    if (messages.Count() == 0) throw new Exception("No new messages for this subscriber");

                    foreach (var msg in messages)
                    {
                        msg.MessageStatus = "REQUESTED";
                    }

                    await context.SaveChangesAsync();
                    return await messages.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving messages: {ex.Message}");
                return null!;
            }
        }


        //public Task<Message> GetSubscriberMessages(MBDbContext context, ushort Id, Subscription Sub)
        //{
        //    throw new NotImplementedException();
        //}

        //public Task<Topic> GetTopicById(MBDbContext context)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
