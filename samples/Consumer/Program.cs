using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace Consumer
{
    public class Message1 : Message { }
    public class Message2 : Message { }
    public class Message3 : Message { }
    public class Message4 : Message { }
    public class Message5 : Message { }
    public class Message6 : Message { }
    public class Message7 : Message { }
    public class Message8 : Message { }
    public class Message9 : Message { }
    public class Message10 : Message { }
    public class Message11 : Message { }
    public class Message12 : Message { }
    public class Message13 : Message { }
    public class Message14 : Message { }
    public class Message15 : Message { }
    public class Message16 : Message { }
    public class Message17 : Message { }
    public class Message18 : Message { }
    public class Message19 : Message { }

    class TestHandlerResolver : IHandlerResolver 
    {
        public IEnumerable<IHandlerAsync<T>> ResolveHandlers<T>() where T : Message
        {
            yield return new ConsoleHandler<T>();
        }
    }

    class ConsoleHandler<T> : IHandlerAsync<T> where T : Message
    {
        public Task<bool> Handle(T message)
        {
            Console.WriteLine($"New message: {message.Id}");
            return Task.FromResult(true);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("receiving messages");

            IHandlerResolver handlerResolver = new TestHandlerResolver();

        var queue = "ConsumerQueue";
            var publisher = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithAwsClientFactory(() => new DefaultAwsClientFactory(new StoredProfileAWSCredentials("dev")))
                // custom naming strategy
                // Will create a queue per incoming per message
                .WithNamingStrategy(() => new CustomNamingStrategy())
                //.WithAwsClientFactory(() => new LocalhostAwsClientFact())
                // listen for Message1: topic Message1, queue ConsumerQueue-Message1
                .WithSqsTopicSubscriber()
                .IntoQueue(queue)
                .WithMessageHandler<Message1>(handlerResolver)
                .WithMessageHandler<Message2>(handlerResolver)
                .WithMessageHandler<Message3>(handlerResolver)
                .WithMessageHandler<Message4>(handlerResolver)
                .WithMessageHandler<Message5>(handlerResolver)
                .WithMessageHandler<Message6>(handlerResolver)
                .WithMessageHandler<Message7>(handlerResolver)
                .WithMessageHandler<Message8>(handlerResolver)
                .WithMessageHandler<Message9>(handlerResolver)
                .WithMessageHandler<Message10>(handlerResolver)
                .WithMessageHandler<Message11>(handlerResolver)
                .WithMessageHandler<Message12>(handlerResolver)
                .WithMessageHandler<Message13>(handlerResolver)
                .WithMessageHandler<Message14>(handlerResolver)
                .WithMessageHandler<Message15>(handlerResolver)
                .WithMessageHandler<Message16>(handlerResolver)
                .WithMessageHandler<Message17>(handlerResolver)
                .WithMessageHandler<Message18>(handlerResolver)
                .WithMessageHandler<Message19>(handlerResolver)
                .WithSnsMessagePublisher<Message1>()
                .WithSnsMessagePublisher<Message2>()

                .WithSqsPointToPointSubscriber()
                .IntoQueue(queue)
                // listen for Message3: queue ConsumerQueue-Message3
                .WithMessageHandler<Message3>(handlerResolver)
                // listen for Message4: queue ConsumerQueue-Message4
                .WithMessageHandler<Message4>(handlerResolver)
                .WithSqsMessagePublisher<Message3>(config => config.QueueName = queue)
                .WithSqsMessagePublisher<Message4>(config => config.QueueName = queue);
                

            publisher.StartListening();
            publisher.Publish(new Message2());
            publisher.Publish(new Message3());

            Console.WriteLine("done");
            Console.ReadKey();
        }
    }
}
