using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenUpdatingRedrivePolicy : WhenCreatingQueuesByName
    {
        private int _newMaximumReceived;

        protected override void Given()
        {
            _newMaximumReceived = 2;

            base.Given();
        }

        protected override void When()
        {
            var config = GetQueueConfig();
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(config);

            config.RetryCountBeforeSendingToErrorQueue = _newMaximumReceived;
            SystemUnderTest.Update(queue, config);
        }

        [Test]
        public void TheRedrivePolicyIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newMaximumReceived, queue.RedrivePolicy.MaximumReceives);
        }
    }
}