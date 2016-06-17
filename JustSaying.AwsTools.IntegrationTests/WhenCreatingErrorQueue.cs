using System;
using System.Threading;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenCreatingErrorQueue : BehaviourTest<AmazonQueueCreator>
    {
        protected string QueueUniqueKey;
        protected ISqsQueue queue;

        protected override void Given()
        { }

        protected ISqsQueueConfig GetQueueConfig()
        {
            return new SqsQueueConfig(RegionEndpoint.EUWest1, QueueUniqueKey);
        }

        protected override void When()
        {
            var config = GetQueueConfig();
            config.ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(config);

            config.ErrorQueueRetentionPeriodSeconds = 100;
            SystemUnderTest.Update(queue, config);
        }

        protected override AmazonQueueCreator CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            var queueVerifier = new AmazonQueueCreator(new AwsClientFactoryProxy(() => CreateMeABus.DefaultClientFactory()));
            queueVerifier.QueueCache = new NullCache<ISqsQueue>();
            queueVerifier.TopicCache = new NullCache<ISnsTopic>();
            return queueVerifier;
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete(queue);
            base.PostAssertTeardown();
        }

        [Test]
        public void TheRetentionPeriodOfTheErrorQueueStaysAsMaximum()
        {
            Assert.AreEqual(100, queue.ErrorQueue.MessageRetentionPeriod);
        }
    }
}