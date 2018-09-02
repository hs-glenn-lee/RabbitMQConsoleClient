using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json; // install-package Newtonsoft.Json
using EasyNetQ; // Install-Package EasyNetQ


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
            /*
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    
                   // channel.QueueDeclare(queue: "hello2",
                  //               durable: false,
                   //              exclusive: false,
                    //             autoDelete: true,
                     //            arguments: null);
                                 

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
            */


            var advBus = RabbitHutch.CreateBus("host=207.148.88.116:5672; virtualHost=created-docs-vhost; username=created-docs-dev; password=rlaehdgus").Advanced;
            var exchange = advBus.ExchangeDeclare("created-docs.direct", ExchangeType.Direct);
            var queue = advBus.QueueDeclare(
                "username-q",
                durable: false,
                exclusive: false,
                autoDelete: true
            );


            Authentication appAuthMessage = new Authentication { username = "123", password="12345", type="normal" };
            string jsonMsg = JsonConvert.SerializeObject(appAuthMessage);
            var body = Encoding.UTF8.GetBytes(jsonMsg);

            var messageProperties = new MessageProperties();
            messageProperties.ContentEncoding = "UTF-8";
            messageProperties.ContentType = "application/json";
            messageProperties.MessageId = System.Guid.NewGuid().ToString();
            messageProperties.CorrelationId = System.Guid.NewGuid().ToString();
            messageProperties.ReplyTo = queue.Name;

            var msg = new Message<Authentication>(appAuthMessage);
            msg.SetProperties(messageProperties);

            advBus.Publish(
                exchange,
                "app.auth",
                false,
                message: msg
            );
            

            Console.WriteLine(" [x] Sent {0}", jsonMsg);
            advBus.Consume(queue, (bbody, properties, info) => Task.Factory.StartNew(() =>
            {
                var message = Encoding.UTF8.GetString(bbody);
                Console.WriteLine("Got message: '{0}'", message);
            }));


            //var bus = RabbitHutch.CreateBus("host=207.148.88.116:5672; virtualHost=created-docs-vhost; username=created-docs-dev; password=rlaehdgus");
            //bus.Subscribe<AppAuthMessage>("subscribe_async_test", ss => Console.WriteLine(ss.from));



            Console.ReadLine();
        }
    }

    class Authentication
    {
        public string username { get; set; }
        public string password { get; set; }
        public string type { get; set; } //
    }

    class AuthenticationResult
    {
        public string resultCode { get; set; } // authorized, unauthorized, needToActivateNew
        public ActivatedSubscription activatedSubscription { get; set; }
    }

    class ActivatedSubscription
    {
        public String id { get; set; }
        public String activatedAt { get; set; }
    }

}
