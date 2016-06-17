using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IQueueCreator
    {
        ISqsQueue Create(ISqsQueueConfig config, int attempt = 0);

        string Exists(RegionEndpoint region, string queueName);
        string Exists(ISqsQueueConfig config);

        void UpdateRedrivePolicy(ISqsQueue queue, RedrivePolicy redrivePolicy);
        void UpdateRedrivePolicy(ISqsQueue queue, ISqsQueueConfig queueConfig, ISqsQueue errorQueue);

        void UpdateAttributes(ISqsQueue queue, Dictionary<string, string> getQueueAttributes);
        void UpdateAttributes(ISqsQueue queue, ISqsQueueConfig config);

        ISqsQueue Find(string url, ISqsQueueConfig config);
        ISqsQueue Find(ISqsQueueConfig config);

        void Delete(ISqsQueue queue);
    }
}