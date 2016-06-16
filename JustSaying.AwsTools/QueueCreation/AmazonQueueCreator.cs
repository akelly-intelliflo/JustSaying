using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Policy;
using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using NLog.Fluent;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly IRegionResourceCache<ISqsQueue> _queueCache = new RegionResourceCache<ISqsQueue>();
        private readonly IRegionResourceCache<ISnsTopic> _topicCache = new RegionResourceCache<ISnsTopic>();
        private readonly IQueueCreator queueCreator;
        private readonly ISnsTopicCreator topicCreator;

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
            queueCreator = new QueueCreator(_awsClientFactory);
            topicCreator = new SnsTopicCreator(_awsClientFactory);
        }

        public ISnsTopic EnsureTopicExistsWithQueueSubscribed(ISqsQueue queue, IMessageSerialisationRegister serialisationRegister, ISnsTopicConfig topicConfig)
        {
            //var queue = EnsureQueueAndErrorQueueExists(queueConfig);
            var eventTopic = EnsureTopicExists(topicConfig, serialisationRegister);
            EnsureQueueIsSubscribedToTopic(eventTopic, queue);

            return eventTopic;
        }

        public ISqsQueue EnsureQueueAndErrorQueueExists(ISqsQueueConfig queueConfig)
        {
            if (queueConfig.Region == null)
                throw new ArgumentNullException(nameof(queueConfig.Region));
            var region = queueConfig.Region;
            var queue = _queueCache.TryGetFromCache(region.ToString(), queueConfig.QueueName);
            if (queue != null)
            {
                return queue;
            }

            var errorQueue = CreateErrorQueue(queueConfig);
            queueConfig.ErrorQueue = errorQueue;


            var queueUrl = queueCreator.Exists(queueConfig);
            queue = string.IsNullOrWhiteSpace(queueUrl) ? queueCreator.CreateQueue(queueConfig) : queueCreator.FindQueue(queueUrl, queueConfig);

            // TODO - merge 2 update attributes statements into one
            if (QueueNeedsUpdating(queue, queueConfig))
                queueCreator.UpdateAttributes(queue, GetQueueAttributes(queueConfig));
            if (RedrivePolicyNeedsUpdating(queue, queueConfig))
                queueCreator.UpdateAttributes(queue, GetRedrivePolicyAttributes(queueConfig, errorQueue));

            _queueCache.AddToCache(region.ToString(), queue.QueueName, queue);
            return queue;
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

        private bool RedrivePolicyNeedsUpdating(ISqsQueue queue, ISqsQueueConfig queueConfig)
        {
            return queue.RedrivePolicy == null || queue.RedrivePolicy.MaximumReceives != queueConfig.RetryCountBeforeSendingToErrorQueue;
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

        private bool QueueNeedsUpdating(ISqsQueue queue, ISqsQueueConfig queueConfig)
        {
            return queue.MessageRetentionPeriod != queueConfig.MessageRetentionSeconds
                   || queue.VisibilityTimeout != queueConfig.VisibilityTimeoutSeconds
                   || queue.DeliveryDelay != queueConfig.DeliveryDelaySeconds;
        }

        private ISqsQueue CreateErrorQueue(ISqsQueueConfig queueConfig)
        {
            var errorConfig = queueConfig.Clone();
            errorConfig.QueueName = errorConfig.QueueName + "_error";
            errorConfig.ErrorQueueOptOut = true;

            var errorQueueUrl = queueCreator.Exists(errorConfig);
            var errorQueue = string.IsNullOrWhiteSpace(errorQueueUrl) ? queueCreator.CreateQueue(errorConfig) : queueCreator.FindQueue(errorQueueUrl, queueConfig);

            if (ErrorQueueNeedsUpdating(errorQueue, errorConfig))
                queueCreator.UpdateAttributes(errorQueue, GetErrorQueueAttributes(errorConfig));
            return errorQueue;
        }


        private Dictionary<string, string> GetErrorQueueAttributes(ISqsQueueConfig queueConfig)
        {
            return new Dictionary<string, string>
            {   
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , queueConfig.ErrorQueueRetentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT.ToString(CultureInfo.InvariantCulture)},
            };
        }

        private bool ErrorQueueNeedsUpdating(ISqsQueue queue, ISqsQueueConfig queueConfig)
        {
            return queue.MessageRetentionPeriod != queueConfig.ErrorQueueRetentionPeriodSeconds;
        }

        private ISnsTopic EnsureTopicExists(ISnsTopicConfig topicConfig, IMessageSerialisationRegister serialisationRegister)
        {
            var eventTopic = _topicCache.TryGetFromCache(topicConfig.Region.SystemName, topicConfig.PublishEndpoint);
            if (eventTopic != null)
                return eventTopic;

            eventTopic = topicCreator.Exists(topicConfig);

            eventTopic = eventTopic ?? topicCreator.CreateTopic(topicConfig);

            _topicCache.AddToCache(topicConfig.Region.SystemName, topicConfig.PublishEndpoint, eventTopic);

            return eventTopic;
        }

        private void EnsureQueueIsSubscribedToTopic(ISnsTopic eventTopic, ISqsQueue queue)
        {
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(queue.Region);
            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(queue.Region);
            
            //var r = new SubscribeRequest(eventTopic.Arn, "sqs", queue.Arn);
            //var subscriptionArn = Client.Subscribe(r);
            var subscriptionArn = snsclient.SubscribeQueue(eventTopic.Arn, sqsclient, queue.Url);
            
            //if (!string.IsNullOrEmpty(subscriptionArn.SubscriptionArn))
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                //SetQueueAttributes(amazonSQSClient, queue);
                //return true;
                return;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, eventTopic.Arn));
            //return false;
        }

        private void SetQueueAttributes(IAmazonSQS amazonSqsClient, SqsQueueBase queue)
        {
            Policy p;
            p = new Policy() { Id = Guid.NewGuid().ToString() };

            var statement = new Statement(Statement.StatementEffect.Allow);
            statement.Actions.Add(new ActionIdentifier(SQSActionIdentifiers.SendMessage.ActionName));
            statement.Resources.Add(new Resource(queue.Arn));
            var newCondition = ConditionFactory.NewCondition(ConditionFactory.ArnComparisonType.ArnLike, ConditionFactory.SOURCE_ARN_CONDITION_KEY, "arn:aws:sns:eu-west-1:963735208092:dev-dariouso-");
            statement.Conditions.Add(newCondition);
            statement.Principals.Add(new Principal("*"));

            p.Statements.Add(statement);


            var request = new SetQueueAttributesRequest()
            {
                QueueUrl = queue.Url,
                Attributes = new Dictionary<string, string> { { "Policy", p.ToJson() } }
            };
            amazonSqsClient.SetQueueAttributes(request);
        }
    }
}