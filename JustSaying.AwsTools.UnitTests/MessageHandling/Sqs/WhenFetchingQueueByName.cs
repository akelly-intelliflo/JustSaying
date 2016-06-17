using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    class WhenFetchingQueueByName
    {
        private IAmazonSQS _client;
        private const int RetryCount = 3;
        private IAwsClientFactoryProxy _factoryProxy;
        private IQueueCreator _queueCreator;

        [SetUp]
        protected void SetUp()
        {
            _client = Substitute.For<IAmazonSQS>();
            _client.ListQueues(Arg.Any<ListQueuesRequest>())
                .Returns(new ListQueuesResponse() { QueueUrls = new List<string>() { "some-queue-name" } });
            _client.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse()
                {
                    Attributes = new Dictionary<string, string>() { { "QueueArn", "something:some-queue-name" } }
                });

            _factoryProxy = new AwsClientFactoryProxy(() => new MockedAwsClientFactory(_client));
            _queueCreator = new QueueCreator(_factoryProxy);
        }

        [Then]
        public void IncorrectQueueNameDoNotMatch()
        {
            Assert.IsEmpty(_queueCreator.Exists(RegionEndpoint.EUWest1, "some-queue-name1"));
        }

        [Then]
        public void IncorrectPartialQueueNameDoNotMatch()
        {
            Assert.IsEmpty(_queueCreator.Exists(RegionEndpoint.EUWest1, "some-queue"));
        }

        [Then]
        public void CorrectQueueNameShouldMatch()
        {
            Assert.IsNotEmpty(_queueCreator.Exists(RegionEndpoint.EUWest1, "some-queue-name"));
        }
    }
}
