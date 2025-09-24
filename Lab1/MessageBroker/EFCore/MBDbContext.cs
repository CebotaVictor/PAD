using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PR_c_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_c_.EFCore
{
    public class MBDbContext:DbContext
    {
        //public MBDbContext(DbContextOptions<MBDbContext> options) : base(options)
        //{
            
        //}

        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Topic> Topics => Set<Topic>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=C:\Users\user\Desktop\my cprojects\UTM\AIV_1\PAD\Lab1\MessageBroker\MessageBroker.db")
            .LogTo(Console.WriteLine, LogLevel.Information);
        }
    }
    
}
