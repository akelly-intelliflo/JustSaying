using System.Collections.Generic;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using NLog.Fluent;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISnsTopicCreator
    {
        ISnsTopic CreateTopic(ISnsTopicConfig config, int attempt = 0);
        ISnsTopic Exists(ISnsTopicConfig config);
        ISnsTopic FindTopic(ISnsTopicConfig topicConfig);
    }

    class SnsTopicCreator : ISnsTopicCreator
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");
        private readonly IAwsClientFactoryProxy _awsClientFactory;

        public SnsTopicCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public ISnsTopic CreateTopic(ISnsTopicConfig config, int attempt = 0)
        {
            var client = _awsClientFactory.GetAwsClientFactory().GetSnsClient(config.Region);

            var response = client.CreateTopic(new CreateTopicRequest(config.Topic));

            var topic = new PlainSnsTopic();
            topic.Arn = response.TopicArn;
            topic.Name = config.Topic;
            Log.Info(string.Format("Created Topic: {0} on Arn: {1}", topic.Name, topic.Arn));
            return topic;
        }

        public ISnsTopic Exists(ISnsTopicConfig config)
        {
            return FindTopic(config);
        }


        public ISnsTopic FindTopic(ISnsTopicConfig topicConfig)
        {
            var client = _awsClientFactory.GetAwsClientFactory().GetSnsClient(topicConfig.Region);

            Log.Info("Checking if topic '{0}' exists", topicConfig.Topic);
            var topic = client.FindTopic(topicConfig.Topic);

            if (topic == null || string.IsNullOrWhiteSpace(topic.TopicArn))
                return null;

            return new PlainSnsTopic()
            {
                Arn = topic.TopicArn,
                Name = topicConfig.Topic
            };
        }
    }

    public class SnsTopicByName : SnsTopicBase2
    {
        public string TopicName { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            : base(serialisationRegister)
        {
            TopicName = topicName;
            Client = client;
        }

        public override bool Exists()
        {
            if (string.IsNullOrWhiteSpace(Arn) == false)
                return true;

            Log.Info("Checking if topic '{0}' exists", TopicName);
            var topic = Client.FindTopic(TopicName);

            if (topic != null)
            {
                Arn = topic.TopicArn;
                return true;
            }
            
            return false;
        }

        public bool Create()
        {
            var response = Client.CreateTopic(new CreateTopicRequest(TopicName));
            if (!string.IsNullOrEmpty(response.TopicArn))
            {    
                Arn = response.TopicArn;
                Log.Info(string.Format("Created Topic: {0} on Arn: {1}", TopicName, Arn));
                return true;
            }

            Log.Info(string.Format("Failed to create Topic: {0}", TopicName));
            return false;
        }
    }
}