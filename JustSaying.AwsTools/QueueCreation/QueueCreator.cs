using System.Collections.Generic;
using System.Globalization;
using System.Security.Policy;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using NLog.Fluent;

namespace JustSaying.AwsTools.QueueCreation
{
    class QueueCreator : IQueueCreator
    {
        private readonly ISqsQueueConfig _config;
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private IAmazonSQS _client;

        public QueueCreator(ISqsQueueConfig config, IAwsClientFactoryProxy awsClientFactory)
        {
            _config = config;
            _awsClientFactory = awsClientFactory;
            _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(config.Region);
        }

        public ISqsQueue CreateQueue(ISqsQueueConfig config)
        {
            return CreateQueue(config, 0);
        }

        public ISqsQueue CreateQueue(ISqsQueueConfig config, int attempt = 0)
        {
            
            try
            {
                var result = _client.CreateQueue(new CreateQueueRequest
                {
                    QueueName = config.QueueName,
                    Attributes = GetCreateQueueAttributes(_config)
                });

                if (!string.IsNullOrWhiteSpace(result.QueueUrl))
                {
                    Url = result.QueueUrl;
                    SetQueueProperties();

                    Log.Info(string.Format("Created Queue: {0} on Arn: {1}", QueueName, Arn));
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Log.Info(string.Format("Waiting to create Queue due to AWS time restriction - Queue: {0}, AttemptCount: {1}", QueueName, attempt + 1));
                    Thread.Sleep(60000);
                    Create(queueConfig, attempt: attempt++);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Log.Error(ex, string.Format("Create Queue error: {0}", QueueName));
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    Log.Error(ex, string.Format("Create Queue error, max retries exceeded for delay - Queue: {0}", QueueName));
                    throw;
                }
            }

            Log.Info(string.Format("Failed to create Queue: {0}", QueueName));
            return false;
        }

        private Dictionary<string, string> GetCreateQueueAttributes(ISqsQueueConfig queueConfig)
        {
            var policy = new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD ,queueConfig.MessageRetentionSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , queueConfig.VisibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_DELAY_SECONDS  , queueConfig.DeliveryDelaySeconds.ToString(CultureInfo.InvariantCulture)},
            };
            if (NeedErrorQueue(queueConfig))
            {
                policy.Add(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, ErrorQueue.Arn).ToString());
            }

            return policy;
        }

        private static bool NeedErrorQueue(ISqsQueueConfig queueConfig)
        {
            return !queueConfig.ErrorQueueOptOut;
        }

        public bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest { QueueNamePrefix = QueueName });
            Log.Info("Checking if queue '{0}' exists", QueueName);
            Url = result.QueueUrls.SingleOrDefault(x => Matches(x, QueueName));

            if (Url != null)
            {
                SetQueueProperties();
                return true;
            }

            return false;
        }
    }
}