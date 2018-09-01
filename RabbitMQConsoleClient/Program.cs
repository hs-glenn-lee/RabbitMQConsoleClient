using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json; // install-package Newtonsoft.Json

namespace RabbitMQConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {

            var factory = new ConnectionFactory() {
                HostName = "207.148.88.116",
                UserName = "created-docs-dev",
                Password = "rlaehdgus",
                VirtualHost = "created-docs-vhost"
            };

            // created-docs-vhost
            // created-docs-dev

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    /*
                    channel.QueueDeclare(queue: "hello2",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: true,
                                 arguments: null);
                                 */

                    string message = "{\"from\":\"app\"}";
                    
                    var body = Encoding.UTF8.GetBytes(message);
                    

                    AppAuthMessage msg = new AppAuthMessage { from = "from app" };
                    string jsonMsg = JsonConvert.SerializeObject(msg);
                    IBasicProperties basicProperties = channel.CreateBasicProperties();
                    basicProperties.ContentEncoding = "UTF-8";
                    basicProperties.ContentType = "application/json";

                    channel.BasicPublish(exchange: "created-docs.direct",
                                         routingKey: "app.auth",
                                         basicProperties: basicProperties,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);

                }
            }

            Console.ReadLine();
        }
    }

    class AppAuthMessage
    {
        public string from { get; set; }
    }
}
