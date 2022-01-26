using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PostService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ListenForEvents();
            CreateHostBuilder(args).Build().Run();
        }

        private static void ListenForEvents()
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += Consumer_Received;

            channel.BasicConsume(queue: "user.postservice",
                                 autoAck: true,
                                 consumer: consumer);
        }

        private static void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var contextOptions = new DbContextOptionsBuilder<PostServiceContext>()
                .UseSqlite(@"Data Source=post.db")
                .Options;

            var dbContext = new PostServiceContext(contextOptions);

            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine(" [x] Received {0}", message);

            var data = JObject.Parse(message);
            var type = e.RoutingKey;
            if (type == "user.add")
            {
                dbContext.Users.Add(new Entities.User()
                {
                    ID = data["id"].Value<int>(),
                    Name = data["name"].Value<string>()
                });
                dbContext.SaveChanges();
            }
            else if (type == "user.update")
            {
                var user = dbContext.Users.First(a => a.ID == data["id"].Value<int>());
                user.Name = data["name"].Value<string>();
                dbContext.SaveChanges();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
