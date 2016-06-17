using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.UnitTests.MessageHandling.Sqs;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreExceptionsInMessageProcessing : AsyncBehaviourTest<AwsTools.MessageHandling.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private readonly IMessageSerialisationRegister _serialisationRegister = 
            Substitute.For<IMessageSerialisationRegister>();
        private IAwsClientFactory _awsClientFactory;
        private int _callCount;

        protected override AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            _awsClientFactory = new MockedAwsClientFactory(_sqs);
            return new AwsTools.MessageHandling.SqsNotificationListener(
                new PlainSqsQueue(RegionEndpoint.EUWest1, ""), 
                _serialisationRegister, 
                Substitute.For<IMessageMonitor>(), 
                _awsClientFactory);
        }

        protected override void Given()
        {
            _serialisationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => { throw new TestException("Test from WhenThereAreExceptionsInMessageProcessing"); });
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
        }

        protected override async Task When()
        {
            SystemUnderTest.Listen();
            await Task.Delay(100);
            SystemUnderTest.StopListening();
            await Task.Yield();
        }

        [Then]
        public void TheListenerDoesNotDie()
        {
            Assert.That(_callCount, Is.GreaterThanOrEqualTo(3));
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            };
        }
    }
}