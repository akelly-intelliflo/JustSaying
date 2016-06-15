using JustSaying;
using JustSaying.AwsTools.QueueCreation;

namespace Consumer
{
    public class CustomNamingStrategy : INamingStrategy
    {
        private string prefix = "dev-justsayingiflo";
        /// <summary>
        /// Will default topic to message type, unless topic prefix is specified in topic name
        /// </summary>
        public string GetTopicName(string topicName, string messageType)
        {
            return string.IsNullOrWhiteSpace(topicName)
                ? $"{prefix}-{messageType}"
                : $"{prefix}-{topicName}-{messageType}";
        }

        /// <summary>
        /// Will default queue to message type, unless topic prefix is specified in topic name
        /// </summary>
        public string GetQueueName(SqsReadConfiguration sqsConfig, string messageType)
        {
            return $"{prefix}-{sqsConfig.BaseQueueName}";

            /*
            return string.IsNullOrWhiteSpace(sqsConfig.BaseQueueName)
                ? messageType
                : sqsConfig.BaseQueueName + "-" + messageType;*/
        }
    }
}