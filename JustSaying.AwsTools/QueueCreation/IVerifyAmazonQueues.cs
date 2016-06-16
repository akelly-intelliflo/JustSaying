using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        /// <summary>
        /// Creates sns topic if not exists.
        /// 
        /// Subscribes queue to topic
        /// </summary>
        ISnsTopic EnsureTopicExistsWithQueueSubscribed(ISqsQueue queue , IMessageSerialisationRegister serialisationRegister, ISnsTopicConfig queueConfig);

        /// <summary>
        /// Creates sqs queue if not exists
        /// </summary>
        ISqsQueue EnsureQueueAndErrorQueueExists(ISqsQueueConfig queueConfig);
    }
}