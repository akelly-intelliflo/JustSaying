using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        /// <summary>
        /// Creates sqs queue if not exists.
        /// Creates sns topic if not exists.
        /// 
        /// Subscribes queue to topic
        /// </summary>
        ISqsQueue EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig);

        /// <summary>
        /// Creates sqs queue if not exists
        /// </summary>
        ISqsQueue EnsureQueueExists(string region, ISqsQueueConfig queueConfig);
    }
}