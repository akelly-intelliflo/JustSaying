using System;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : JustSayingFluentlyTestBase
    {
        private readonly IHandlerAsync<Message> _handler = Substitute.For<IHandlerAsync<Message>>();
        private object _response;

        protected override void Given()
        {
        }

        protected override void When()
        {
            _response = SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoQueue(string.Empty)
                .ConfigureSubscriptionWith(cfg => { })
                .WithMessageHandler(_handler);
        }

        [Then]
        public void TheTopicAndQueueIsCreatedInDefaultRegion()
        {
            var region = "defaultRegion";
            QueueVerifier.Received().EnsureQueueAndErrorQueueExists(Arg.Is<ISqsQueueConfig>(x => x.Region.SystemName == region));
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribed(Arg.Any<ISqsQueue>(), Bus.SerialisationRegister, Arg.Is<ISnsTopicConfig>(c => c.Region.SystemName == region));
        }

        [Then]
        public void TheTopicAndQueueIsCreatedInFailoverRegion()
        {
            var region = "failoverRegion";
            QueueVerifier.Received().EnsureQueueAndErrorQueueExists(Arg.Is<ISqsQueueConfig>(x => x.Region.SystemName == region));
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribed(Arg.Any<ISqsQueue>(), Bus.SerialisationRegister, Arg.Is<ISnsTopicConfig>(c => c.Region.SystemName == region));
        }

        [Then]
        public void TheSubscriptionIsCreatedInEachRegion()
        {
            Bus.Received(2).AddNotificationSubscriber(Arg.Any<string>(), Arg.Any<INotificationSubscriber>());
        }

        [Then]
        public void HandlerIsAddedToBus()
        {
            Bus.Received().AddMessageHandler(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<IHandlerAsync<Message>>>());
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            Bus.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser>());
        }

        [Then]
        public void ICanContinueConfiguringTheBus()
        {
            Assert.IsInstanceOf<IFluentSubscription>(_response);
        }
    }
}
