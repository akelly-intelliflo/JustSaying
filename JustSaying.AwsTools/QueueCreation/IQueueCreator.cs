using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IQueueCreator
    {
        ISqsQueue CreateQueue(ISqsQueueConfig config);
        ISqsQueue CreateQueue(ISqsQueueConfig config, int attempt);
    }
}