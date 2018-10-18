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
            //Mac Address를 얻는 복붙코드
            String firstMacAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            // Mac Address를 clientId에 할당한다. clinetId는 중복로그인 구분을 위한 key 값으로 쓰인다.
            var clientId = firstMacAddress;

            //create Bus로 rabbitMQ 브로커에 접속한다.
            var advBus = RabbitHutch.CreateBus("host=207.148.88.116:5672; virtualHost=created-docs-vhost; username=created-docs-dev; password=rlaehdgus").Advanced;
            
            // exchange는 메세지를 분류해 주는 곳으로, 이미 있는 exchange를 새로 declare해도 문제 없다.
            var exchange = advBus.ExchangeDeclare("created-docs.direct", ExchangeType.Direct);


            //ExchangeDeclare는 client가 사용할 임시 큐(접속이 끊기면 없어짐)를 생성한다. 여기서 선언한 큐는 auth 메세지에 대한 답장을 받기위해 쓰인다.
            //서버가 이 큐로 답장 메세지를 보낸다.
            var queue = advBus.QueueDeclare(
                "client." + clientId + ".auth",
                durable: false,
                exclusive: false,
                autoDelete: true
            );

            // 이 큐는 다른 곳에서 로그인 했을 때, 이 클라이언트로 접속금지 메세지를 보내기 위해 선언한다. 이것도 임시 큐
            var unauthQueue = advBus.QueueDeclare(
                "client."+clientId+".unauth",
                durable: false,
                exclusive: false,
                autoDelete: true
            );
            // unauthQueue와 created-docs.direct exchange를 bind 해준다.
            //서버에서 default exchange 발송해야한다. 이렇게 bind하면 auto delete가 안된다.
            //advBus.Bind(exchange, unauthQueue, "client." + clientId + ".unauth");

            // Authentication이란 클래스를 json으로 만들어 메세지의 body로 쓸것이다.
            Authentication appAuthMessage = new Authentication { username = "123", password="12345", type=Authentication.NORMAL, clientId=clientId };
            string jsonMsg = JsonConvert.SerializeObject(appAuthMessage);
            var body = Encoding.UTF8.GetBytes(jsonMsg);

            //메세지의 속성을 입력하는 코드
            var messageProperties = new MessageProperties();
            messageProperties.ContentEncoding = "UTF-8"; //반드시 필요하다.
            messageProperties.ContentType = "application/json"; //반드시 필요하다.
            messageProperties.MessageId = System.Guid.NewGuid().ToString(); // Optional 메세지 관리를 위한 것
            messageProperties.CorrelationId = System.Guid.NewGuid().ToString(); // 답장을 위해 반드시 필요하다.
            messageProperties.ReplyTo = queue.Name; // 이 큐로 답장을 보내달라는 것으로 반드시 필요하다.

            var msg = new Message<Authentication>(appAuthMessage);
            msg.SetProperties(messageProperties);
            
            // 메세지 발송
            advBus.Publish(
                exchange,
                "app.auth",
                false,
                message: msg
            );
            
            // 발송한 메세지 출력
            Console.WriteLine(" [x] Sent {0}", jsonMsg);

            // 해당 큐 (여기서는 아까 만든 답장을 위한 임시큐) 로 메세지가 도착하면 처리하는 코드
            advBus.Consume(queue, (bbody, properties, info) => Task.Factory.StartNew(() =>
            {
                var message = Encoding.UTF8.GetString(bbody);
                Console.WriteLine("Got message: '{0}'", message);
            }));


            //var bus = RabbitHutch.CreateBus("host=207.148.88.116:5672; virtualHost=created-docs-vhost; username=created-docs-dev; password=rlaehdgus");
            //bus.Subscribe<AppAuthMessage>("subscribe_async_test", ss => Console.WriteLine(ss.from));


            // 콘솔 안꺼지게 하기
            Console.ReadLine();
        }
    }

    //Authentication은 클라이언트 -> 서버 로 보내는 메세지. json으로 만들어 보낸다.
    class Authentication
    {
        public static String NORMAL = "NORMAL"; //일반 로그인
        public static String ENFORCED = "ENFORCED"; // 중복로그인 -> 강제로그인
        public static String ACTIVATE_NEW = "ACTIVATE_NEW"; // 이전 구독이 만료되고, 활성화 가능한 구독이 있을 때 이를 활성화 시키면서 로그인

        public string username { get; set; }
        public string password { get; set; }
        public string type { get; set; } //
        public string clientId { get; set; } 
    }

    //AuthencationResult는 서버 -> 클라이언트. Authencation에 대한 답장. json메세지를 이 클래스로 parsing한다.
    class AuthenticationResult
    {
        public static String AUTHORIZED = "AUTHORIZED"; //인증성공
		public static String UNAUHORIZED = "UNAUHORIZED"; //인증실패
		public static String ERROR = "ERROR"; //에러 , message 필드에 에러 원인이 입력되있다.
		public static String NEED_TO_ACTIVATE_NEW = "NEED_TO_ACTIVATE_NEW"; // 새 구독 활성화 필요
		public static String DUPLICATED = "DUPLICATED"; // 로그인 중복, 강제 로그인 해야함

        public string resultCode { get; set; } // authorized, unauthorized, needToActivateNew
        public ActivatedSubscription activatedSubscription { get; set; }    //현재 활성화된 구독 정보
        public ActivatedSubscription message { get; set; } // 메세지.. 에러 메세지로만 쓰일듯
    }

    //활성화된 구독 정보
    class ActivatedSubscription
    {
        public String id { get; set; }
        public String activatedAt { get; set; }
    }

}
