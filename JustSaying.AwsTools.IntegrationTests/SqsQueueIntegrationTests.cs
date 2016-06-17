using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools.IntegrationTests
{
    public abstract class WhenCreatingQueuesByName : BehaviourTest<AmazonQueueCreator>
    {
        protected string QueueUniqueKey;
        protected ISqsQueue queue;

        protected override void Given()
        { }

        protected ISqsQueueConfig GetQueueConfig()
        {
            return new SqsQueueConfig(RegionEndpoint.EUWest1, QueueUniqueKey);
        }

        protected override AmazonQueueCreator CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            //var queue = new SqsQueueByName(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1), 1);
            //queue.Exists();
            //return queue;
            var queueCreator = new AmazonQueueCreator(new AwsClientFactoryProxy(() => CreateMeABus.DefaultClientFactory()));
            queueCreator.QueueCache = new NullCache<ISqsQueue>();
            queueCreator.TopicCache = new NullCache<ISnsTopic>();
            return queueCreator;
        }

        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete(queue);
            base.PostAssertTeardown();
        }
    }
}
