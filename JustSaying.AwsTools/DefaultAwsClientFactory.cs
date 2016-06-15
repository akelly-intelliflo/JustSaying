using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public class DefaultAwsClientFactory : IAwsClientFactory
    {
        private readonly AWSCredentials credentials;

        public DefaultAwsClientFactory()
        {
            credentials = new StoredProfileAWSCredentials("justsaying.integration");
        }

        public DefaultAwsClientFactory(AWSCredentials customCredentials)
        {
            credentials = customCredentials;
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            return AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(credentials, region);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            return AWSClientFactory.CreateAmazonSQSClient(credentials, region);
        }
    }
}