using System;
using System.Configuration;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public enum SubscriptionType { ToTopic, PointToPoint };

    public interface ISqsQueueConfig
    {
        RegionEndpoint Region { get; set; }
        int RetryCountBeforeSendingToErrorQueue { get; }
        string QueueName { get; set; }
        bool ErrorQueueOptOut { get; set; }
        int MessageRetentionSeconds { get; }
        int VisibilityTimeoutSeconds { get; }
        int DeliveryDelaySeconds { get; }
        ISqsQueue ErrorQueue { get; set; }
        int ErrorQueueRetentionPeriodSeconds { get; set; }
        ISqsQueueConfig Clone();
    }

    public interface ISnsTopicConfig
    {
        RegionEndpoint Region { get; set; }
        string Topic { get; set; }
        string PublishEndpoint { get; set; }
    }


    public class SqsReadConfiguration : SqsBasicConfiguration, ISqsQueueConfig, ISnsTopicConfig
    {
        public SqsReadConfiguration(SubscriptionType subscriptionType)
        {
            SubscriptionType = subscriptionType;
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
        }

        public SubscriptionType SubscriptionType { get; private set; }

        public string BaseQueueName { get; set; }
        public string QueueName { get; set; }
        public ISqsQueueConfig Clone()
        {
            return MemberwiseClone() as ISqsQueueConfig;
        }

        /// <summary>
        /// TODO - not set anywhere
        /// </summary>
        public RegionEndpoint Region { get; set; }
        public ISqsQueue ErrorQueue { get; set; }

        public string BaseTopicName { get; set; }
        public string Topic { get; set; }
        public string PublishEndpoint { get; set; }

        public int? InstancePosition { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception, Amazon.SQS.Model.Message> OnError { get; set; }

        public override void Validate()
        {
            ValidateSqsConfiguration();
            ValidateSnsConfiguration();
        }

        public void ValidateSqsConfiguration()
        {
            base.Validate();

            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
            {
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");
            }
        }

        private void ValidateSnsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(Topic))
            {
                throw new ConfigurationErrorsException("Invalid configuration. Topic must be provided.");
            }

            if (PublishEndpoint == null)
            {
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
            }
        }
    }
}