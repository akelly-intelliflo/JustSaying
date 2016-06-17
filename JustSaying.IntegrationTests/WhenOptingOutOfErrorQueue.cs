using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.IntegrationTests
{
    public class OrderPlacedHandler : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message)
        {
            return Task.FromResult(true);
        }
    }

    public class WhenOptingOutOfErrorQueue
    {
        private IAmazonSQS _client;

        [SetUp]
        public void SetUp()
        {
            _client = CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1);
        }

        [Test]
        public void ErrorQueueShouldNotBeCreated()
        {
            var queueName = "test-queue-issue-191";
            CreateMeABus.InRegion("eu-west-1")
                .WithSnsMessagePublisher<GenericMessage>()

                .WithSqsTopicSubscriber()
                .IntoQueue(queueName)
                .ConfigureSubscriptionWith(policy =>
                {
                    policy.ErrorQueueOptOut = true;
                })
                .WithMessageHandler(new OrderPlacedHandler());

            AssertThatQueueDoesNotExist(queueName+ "_error");
        }

        private void AssertThatQueueDoesNotExist(string name)
        {
            var queueCreator = new QueueCreator(new AwsClientFactoryProxy(() => CreateMeABus.DefaultClientFactory()));
            var config = new SqsQueueConfig(RegionEndpoint.EUWest1, name);
            Assert.IsEmpty(queueCreator.Exists(config), string.Format("Expecting queue '{0}' to not exist but it does.", name));
        }
    }
}