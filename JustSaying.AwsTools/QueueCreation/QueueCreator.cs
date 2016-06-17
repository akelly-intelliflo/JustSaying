using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using NLog;

namespace JustSaying.AwsTools.QueueCreation
{
    public class QueueCreator : IQueueCreator
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");
        private readonly IAwsClientFactoryProxy _awsClientFactory;

        public QueueCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public ISqsQueue Create(ISqsQueueConfig config, int attempt = 0)
        {
            try
            {
                var _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(config.Region);
                var result = _client.CreateQueue(new CreateQueueRequest
                {
                    QueueName = config.QueueName,
                    Attributes = GetCreateQueueAttributes(config)
                });

                var queue = Find(result.QueueUrl, config);

                Log.Info(string.Format("Created Queue: {0} on Arn: {1}", queue.QueueName, queue.Arn));
                return queue;
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // If we're on a delete timeout, throw after 2 attempts.
                    if (attempt >= 2)
                    {
                        Log.Error(ex, string.Format("Create Queue error, max retries exceeded for delay - Queue: {0}", config.QueueName));
                        throw;
                    }

                    // Ensure we wait for queue delete timeout to expire.
                    Log.Info(string.Format("Waiting to create Queue due to AWS time restriction - Queue: {0}, AttemptCount: {1}", config.QueueName, attempt + 1));
                    Thread.Sleep(60000);
                    return Create(config, attempt: attempt++);
                }

                // Throw all errors which are not delete timeout related.
                Log.Error(ex, string.Format("Create Queue error: {0}", config.QueueName));
                throw;
            }
        }

        private Dictionary<string, string> GetCreateQueueAttributes(ISqsQueueConfig _config)
        {
            var policy = new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD ,_config.MessageRetentionSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , _config.VisibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_DELAY_SECONDS  , _config.DeliveryDelaySeconds.ToString(CultureInfo.InvariantCulture)},
            };
            if (NeedErrorQueue(_config))
            {
                policy.Add(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, new RedrivePolicy(_config.RetryCountBeforeSendingToErrorQueue, _config.ErrorQueue.Arn).ToString());
            }

            return policy;
        }

        private bool NeedErrorQueue(ISqsQueueConfig _config)
        {
            return !_config.ErrorQueueOptOut;
        }

        private static bool Matches(string queueUrl, string queueName)
        {
            return queueUrl.Substring(queueUrl.LastIndexOf("/", StringComparison.InvariantCulture) + 1)
                .Equals(queueName, StringComparison.InvariantCultureIgnoreCase);
        }

        public ISqsQueue Find(ISqsQueueConfig config)
        {
            var url = Exists(config);
            if (string.IsNullOrWhiteSpace(url))
                return null;
            return Find(url, config);
        }

        public ISqsQueue Find(string url, RegionEndpoint region)
        {
            // TODO - fix
            return Find(url, new SqsQueueConfig(region, "dummyName"));
        }


        public ISqsQueue Find(RegionEndpoint region, string name)
        {
            var exists = Exists(region, name);
            return Find(exists, region);
        }

        /// <summary>
        /// TODO - config here is not needed, only region. Name can be extracted
        /// </summary>
        public ISqsQueue Find(string url, ISqsQueueConfig config)
        {
            var attributes = GetAttrs(url, config.Region, new[]
            {
                JustSayingConstants.ATTRIBUTE_ARN,
                JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY,
                JustSayingConstants.ATTRIBUTE_POLICY,
                JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD,
                JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT,
                JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY
            });
            if (attributes == null)
                return null;

            var sqs = new PlainSqsQueue(config.Region, config.QueueName);
            sqs.Url = url;
            sqs.Arn = attributes.QueueARN;
            sqs.MessageRetentionPeriod = attributes.MessageRetentionPeriod;
            sqs.VisibilityTimeout = attributes.VisibilityTimeout;
            sqs.DeliveryDelay = attributes.DelaySeconds;
            sqs.RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
            return sqs;
        }

        private RedrivePolicy ExtractRedrivePolicyFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (!queueAttributes.ContainsKey(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY))
            {
                return null;
            }
            return RedrivePolicy.ConvertFromString(queueAttributes[JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY]);
        }

        protected GetQueueAttributesResult GetAttrs(string url, RegionEndpoint region, IEnumerable<string> attrKeys)
        {
            var _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            var request = new GetQueueAttributesRequest
            {
                QueueUrl = url,
                AttributeNames = new List<string>(attrKeys)
            };

            var result = _client.GetQueueAttributes(request);

            return result;
        }

        public string Exists(RegionEndpoint region, string queueName)
        {
            var _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            var result = _client.ListQueues(new ListQueuesRequest { QueueNamePrefix = queueName });
            Console.WriteLine($"Checking if queue '{queueName}' exists");
            Log.Info("Checking if queue '{0}' exists", queueName);
            var url = result.QueueUrls.SingleOrDefault(x => Matches(x, queueName));

            if (url != null)
            {
                // TODO set properties
                //SetQueueProperties();
                return url;
            }

            return string.Empty;
        }

        public string Exists(ISqsQueueConfig _config)
        {
            return Exists(_config.Region, _config.QueueName);
        }

        public void UpdateAttributes(ISqsQueue queue, ISqsQueueConfig config)
        {
            UpdateAttributes(queue, GetQueueAttributes(config));
            queue.MessageRetentionPeriod = config.MessageRetentionSeconds;
            queue.VisibilityTimeout = config.VisibilityTimeoutSeconds;
            queue.DeliveryDelay = config.DeliveryDelaySeconds;
        }

        private Dictionary<string, string> GetQueueAttributes(ISqsQueueConfig queueConfig)
        {
            return new Dictionary<string, string>
            {
                {JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD, queueConfig.MessageRetentionSeconds.ToString()},
                {JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, queueConfig.VisibilityTimeoutSeconds.ToString()},
                {JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY, queueConfig.DeliveryDelaySeconds.ToString()}
            };
        }

        public void UpdateAttributes(ISqsQueue queue, Dictionary<string, string> getErrorQueueAttributes)
        {
            var _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(queue.Region);
            var request = new SetQueueAttributesRequest
            {
                QueueUrl = queue.Url,
                Attributes = getErrorQueueAttributes
            };
            var response = _client.SetQueueAttributes(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new Exception($"Could not update queue {queue.QueueName} attributes. Response status: {response.HttpStatusCode}");
        }

        public void Delete(ISqsQueue queue)
        {
            var _client = _awsClientFactory.GetAwsClientFactory().GetSqsClient(queue.Region);
            _client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queue.Url });
        }

        public void UpdateRedrivePolicy(ISqsQueue queue, RedrivePolicy redrivePolicy)
        {
            UpdateAttributes(queue, GetRedrivePolicyAttributes(redrivePolicy));
        }

        public void UpdateRedrivePolicy(ISqsQueue queue, ISqsQueueConfig queueConfig, ISqsQueue errorQueue)
        {
            UpdateAttributes(queue, GetRedrivePolicyAttributes(queueConfig, errorQueue));
            queue.RedrivePolicy.MaximumReceives = queueConfig.RetryCountBeforeSendingToErrorQueue;
        }

        private Dictionary<string, string> GetRedrivePolicyAttributes(RedrivePolicy redrivePolicy)
        {
            return new Dictionary<string, string>
            {
                {
                    JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, redrivePolicy.ToString()
                }
            };
        }

        private Dictionary<string, string> GetRedrivePolicyAttributes(ISqsQueueConfig queueConfig, ISqsQueue errorQueue)
        {
            return new Dictionary<string, string>
            {
                {
                    JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, errorQueue.Arn).ToString()
                }
            };
        }
    }
}