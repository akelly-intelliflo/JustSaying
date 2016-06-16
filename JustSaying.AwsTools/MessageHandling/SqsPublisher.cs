using System.Security.Policy;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{

    /// <summary>
    /// TODO rename
    /// </summary>
    public class PlainSqsPublisher : IMessagePublisher
    {
        private readonly ISqsQueue _queue;
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public PlainSqsPublisher(ISqsQueue queue, IAmazonSQS client, IMessageSerialisationRegister serialisationRegister)
        {
            _queue = queue;
            _client = client;
            _serialisationRegister = serialisationRegister;
        }

        public void Publish(Message message)
        {
            _client.SendMessage(new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = _queue.Url
            });
        }

        public string GetMessageInContext(Message message)
        {
            return _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
        }
    }

    /// <summary>
    /// TODO - obsolete
    /// </summary>
    public class SqsPublisher2 : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;

        public SqsPublisher2(RegionEndpoint region, string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister)
            : base(region, queueName, client, retryCountBeforeSendingToErrorQueue)
        {
            _client = client;
            _serialisationRegister = serialisationRegister;
        }

        public void Publish(Message message)
        {
            _client.SendMessage(new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = Url
            });
        }

        public string GetMessageInContext(Message message)
        {
            return _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
        }
    }
}