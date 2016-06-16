using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IQueueCreator
    {
        ISqsQueue CreateQueue(ISqsQueueConfig config, int attempt = 0);
        string Exists(ISqsQueueConfig config);
        void UpdateAttributes(ISqsQueue queue, Dictionary<string, string> getQueueAttributes);
        ISqsQueue FindQueue(string url, ISqsQueueConfig config);
    }
}