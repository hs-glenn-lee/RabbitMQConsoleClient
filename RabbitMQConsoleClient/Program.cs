using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

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

                    string message = "Hello World!";
                    var body = Encoding.UTF8.GetBytes(message);


                    channel.BasicPublish(exchange: "amq.direct",
                                         routingKey: "created.docs.server.q",
                                         basicProperties: null,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);

                }
            }

            Console.ReadLine();
        }
    }
}
