using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(Arn));
            
            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
        }

        public bool Subscribe(IAmazonSQS amazonSQSClient, SqsQueueBase queue)
        {
            var r= new SubscribeRequest(Arn, "sqs", queue.Arn);
            //var subscriptionArn = Client.Subscribe(r);
            var subscriptionArn = Client.SubscribeQueue(Arn, amazonSQSClient, queue.Url);
            //if (!string.IsNullOrEmpty(subscriptionArn.SubscriptionArn))
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                //SetQueueAttributes(amazonSQSClient, queue);

                return true;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
            return false;
        }

        private void SetQueueAttributes(IAmazonSQS amazonSqsClient, SqsQueueBase queue)
        {
            Policy p;
            p = new Policy() {Id = Guid.NewGuid().ToString()};

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
                Attributes = new Dictionary<string, string> {{"Policy", p.ToJson()}}
            };
            amazonSqsClient.SetQueueAttributes(request);
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.Serialise(message, serializeForSnsPublishing:true);
            var messageType = message.GetType().Name;

            Client.Publish(new PublishRequest
                {
                    Subject = messageType,
                    Message = messageToSend,
                    TopicArn = Arn
                });

            EventLog.Info("Published message: '{0}' with content {1}", messageType, messageToSend);
        }
    }
}