using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json; // install-package Newtonsoft.Json
using EasyNetQ; // Install-Package EasyNetQ
using System.Net.NetworkInformation;


namespace RabbitMQConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //get mac address
            String firstMacAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            var clientId = firstMacAddress;

            var advBus = RabbitHutch.CreateBus("host=207.148.88.116:5672; virtualHost=created-docs-vhost; username=created-docs-dev; password=rlaehdgus").Advanced;
            var exchange = advBus.ExchangeDeclare("created-docs.direct", ExchangeType.Direct);
            var queue = advBus.QueueDeclare(
                "client." + clientId + ".auth",
                durable: false,
                exclusive: false,
                autoDelete: true
            );

            var unauthQueue = advBus.QueueDeclare(
                "client."+clientId+".unauth",
                durable: false,
                exclusive: false,
                autoDelete: true
            );
            advBus.Bind(exchange, unauthQueue, "client." + clientId + ".unauth");


            Authentication appAuthMessage = new Authentication { username = "123", password="12345", type=Authentication.NORMAL, clientId=clientId };
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
        public static String NORMAL = "NORMAL";
        public static String ENFORCED = "ENFORCED";
        public static String ACTIVATE_NEW = "ACTIVATE_NEW";

        public string username { get; set; }
        public string password { get; set; }
        public string type { get; set; } //
        public string clientId { get; set; } 
    }

    class AuthenticationResult
    {
        public static String AUTHORIZED = "AUTHORIZED";
		public static String UNAUHORIZED = "UNAUHORIZED";
		public static String ERROR = "ERROR";
		public static String NEED_TO_ACTIVATE_NEW = "NEED_TO_ACTIVATE_NEW";
		public static String DUPLICATED = "DUPLICATED";

        public string resultCode { get; set; } // authorized, unauthorized, needToActivateNew
        public ActivatedSubscription activatedSubscription { get; set; }
        public ActivatedSubscription message { get; set; }
    }

    class ActivatedSubscription
    {
        public String id { get; set; }
        public String activatedAt { get; set; }
    }

}
