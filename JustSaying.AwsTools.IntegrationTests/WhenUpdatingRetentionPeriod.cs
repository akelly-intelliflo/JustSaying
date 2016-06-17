using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenUpdatingRetentionPeriod : WhenCreatingQueuesByName
    {
        private int _oldRetentionPeriod;
        private int _newRetentionPeriod;

        protected override void Given()
        {
            _oldRetentionPeriod = 600;
            _newRetentionPeriod = 700;

            base.Given();
        }

        protected override void When()
        {
            var config = GetQueueConfig();
            config.MessageRetentionSeconds = _oldRetentionPeriod;
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(config);


            config.MessageRetentionSeconds = _newRetentionPeriod;
            SystemUnderTest.Update(queue, config);
        }

        [Test]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newRetentionPeriod, queue.MessageRetentionPeriod);
        }
    }
}