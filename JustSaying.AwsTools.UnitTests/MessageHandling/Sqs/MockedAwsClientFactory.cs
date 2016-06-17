using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    public class MockedAwsClientFactory : IAwsClientFactory
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IAmazonSimpleNotificationService _snsClient;

        public MockedAwsClientFactory(IAmazonSQS sqsClient, IAmazonSimpleNotificationService snsClient = null)
        {
            _sqsClient = sqsClient;
            _snsClient = snsClient;
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            return _snsClient;
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            return _sqsClient;
        }
    }
}