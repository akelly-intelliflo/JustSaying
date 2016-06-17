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
    public interface ISnsTopic
    {
        string Arn { get; set; }
        string Name { get; set; }
    }

    public class PlainSnsTopic : ISnsTopic
    {
        public string Arn { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// TODO - why is this a publiher??
    /// </summary>
    public abstract class SnsTopicBase2 : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicBase2(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(Arn));
            
            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
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