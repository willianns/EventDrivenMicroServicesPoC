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

        private static IModel _channel;

        private static void ListenForEvents()
        {
            //improvements that can be made
            // * move consumer and acknowledgment to BackgroundService

            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += Consumer_Received;

            _channel.BasicConsume(queue: "user.postservice",
                                 autoAck: false,
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
                if (dbContext.Users.Any(a => a.ID == data["id"].Value<int>()))
                {
                    Console.WriteLine("Ignoring old/duplicate entity");
                }
                else
                {
                    dbContext.Users.Add(new Entities.User()
                    {
                        ID = data["id"].Value<int>(),
                        Name = data["name"].Value<string>(),
                        Version = data["version"].Value<int>(),
                    });
                    dbContext.SaveChanges();
                }
            }
            else if (type == "user.update")
            {
                int newVersion = data["version"].Value<int>();
                var user = dbContext.Users.First(a => a.ID == data["id"].Value<int>());
                if (user.Version >= newVersion)
                {
                    Console.WriteLine("Ignoring old/duplicate entity");
                }
                else
                {
                    user.Name = data["name"].Value<string>();
                    user.Version = newVersion;
                    dbContext.SaveChanges();
                }
            }
            _channel.BasicAck(e.DeliveryTag, false);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
