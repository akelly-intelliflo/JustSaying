using System;
using System.Configuration;
using Amazon;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public enum SubscriptionType { ToTopic, PointToPoint };

    public interface ISqsQueueConfig
    {
        RegionEndpoint Region { get; }
        int RetryCountBeforeSendingToErrorQueue { get; }
        string QueueName { get; }
        bool ErrorQueueOptOut { get; }
        int MessageRetentionSeconds { get; }
        int VisibilityTimeoutSeconds { get; }
        int DeliveryDelaySeconds { get; }
    }

    public class SqsReadConfiguration : SqsBasicConfiguration, ISqsQueueConfig
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

        /// <summary>
        /// TODO - not set anywhere
        /// </summary>
        public RegionEndpoint Region { get; }

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