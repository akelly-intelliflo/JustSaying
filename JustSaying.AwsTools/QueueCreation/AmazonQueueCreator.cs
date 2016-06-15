using System;
using System.Collections.Generic;
using System.Security.Policy;
using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using NLog.Fluent;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly IRegionResourceCache<ISqsQueue> _queueCache = new RegionResourceCache<ISqsQueue>();
        private readonly IRegionResourceCache<SnsTopicByName> _topicCache = new RegionResourceCache<SnsTopicByName>();

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public ISqsQueue EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var queue = EnsureQueueExists(region, queueConfig);
            var eventTopic = EnsureTopicExists(regionEndpoint, serialisationRegister, queueConfig);
            EnsureQueueIsSubscribedToTopic(regionEndpoint, eventTopic, queue);

            return queue;
        }

        public ISqsQueue EnsureQueueExists(string region, ISqsQueueConfig queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var queue = _queueCache.TryGetFromCache(region, queueConfig.QueueName);
            if (queue != null)
            {
                return queue;
            }
            var queueCreator = new QueueCreator(queueConfig, _awsClientFactory);
            queue = queueCreator.CreateQueue(queueConfig);
            

            _queueCache.AddToCache(region, queue.QueueName, queue);
            return queue;
        }

       

        public void EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdated(SqsBasicConfiguration queueConfig)
        {
            if (!Exists())
                Create(queueConfig);
            else
            {
                UpdateQueueAttribute(queueConfig);
            }

            //Create an error queue for existing queues if they don't already have one
            if (ErrorQueue != null && NeedErrorQueue(queueConfig))
            {
                var errorQueueConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
                {
                    ErrorQueueRetentionPeriodSeconds = queueConfig.ErrorQueueRetentionPeriodSeconds,
                    ErrorQueueOptOut = true
                };
                if (!ErrorQueue.Exists())
                {

                    ErrorQueue.Create(errorQueueConfig);
                }
                else
                {
                    ErrorQueue.UpdateQueueAttribute(errorQueueConfig);
                }
            }
            UpdateRedrivePolicy(new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, ErrorQueue.Arn));

        }

        private SnsTopicByName EnsureTopicExists(RegionEndpoint region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {

            var eventTopic = _topicCache.TryGetFromCache(region.SystemName, queueConfig.PublishEndpoint);
            if (eventTopic != null)
                return eventTopic;

            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(region);
            eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);
            _topicCache.AddToCache(region.SystemName, queueConfig.PublishEndpoint, eventTopic);

            if (!eventTopic.Exists())
            {
                eventTopic.Create();
            }

            return eventTopic;
        }

        private void EnsureQueueIsSubscribedToTopic(RegionEndpoint region, SnsTopicByName eventTopic, ISqsQueue queue)
        {
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            Subscribe(eventTopic, sqsclient, queue);
        }

        public bool Subscribe(SnsTopicByName eventTopic, IAmazonSQS amazonSQSClient, ISqsQueue queue)
        {
            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(queue.Region);
            
            //var r = new SubscribeRequest(eventTopic.Arn, "sqs", queue.Arn);
            //var subscriptionArn = Client.Subscribe(r);
            var subscriptionArn = snsclient.SubscribeQueue(eventTopic.Arn, amazonSQSClient, queue.Url);
            
            //if (!string.IsNullOrEmpty(subscriptionArn.SubscriptionArn))
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                //SetQueueAttributes(amazonSQSClient, queue);

                return true;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, eventTopic.Arn));
            return false;
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