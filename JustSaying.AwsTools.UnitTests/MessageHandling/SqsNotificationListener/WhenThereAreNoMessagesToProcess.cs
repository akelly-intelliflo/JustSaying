using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.UnitTests.MessageHandling.Sqs;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreNoMessagesToProcess : AsyncBehaviourTest<AwsTools.MessageHandling.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private int _callCount;
        private IAwsClientFactory _awsClientFactory;

        protected override AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            _awsClientFactory = new MockedAwsClientFactory(_sqs);

            var queue = new PlainSqsQueue(RegionEndpoint.EUWest1, ""); 
            return new AwsTools.MessageHandling.SqsNotificationListener(
                queue,
                null,
                Substitute.For<IMessageMonitor>(),
                _awsClientFactory);
        }

        protected override void Given()
        {
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x =>  x.ReceiveMessageAsync(
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
        public void ListenLoopDoesNotDie()
        {
            Assert.That(_callCount, Is.GreaterThan(3));
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { Messages = new List<Message>() };
        }
    }
}