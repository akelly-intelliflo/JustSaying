using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace Consumer
{
    public class LocalhostAwsClientFact : IAwsClientFactory
    {
        private BasicAWSCredentials credentials;

        public LocalhostAwsClientFact()
        {
            credentials = new BasicAWSCredentials("AKIAJWAXB6LKWDQBZGXA", "UlvE4qxHf4UjRAtSib9Qsat64+hTtSKLM6f9Up48");
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            AmazonSimpleNotificationServiceConfig config = new AmazonSimpleNotificationServiceConfig();
            config.ServiceURL = "http://localhost:44334/sns/";
            return new AmazonSimpleNotificationServiceClient(credentials, config);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var config = new AmazonSQSConfig();
            config.ServiceURL = "http://localhost:44334/sqs/";
            return new AmazonSQSClient(credentials, config);
        }
    }
}