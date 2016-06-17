using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenUpdatingDeliveryDelay : WhenCreatingQueuesByName
    {
        private int _oldDeliveryDelay;
        private int _newDeliveryDelay;

        protected override void Given()
        {
            _oldDeliveryDelay = 120;
            _newDeliveryDelay = 300;

            base.Given();
        }

        protected override void When()
        {
            var config = GetQueueConfig();
            config.DeliveryDelaySeconds = _oldDeliveryDelay;

            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(config);

            config.DeliveryDelaySeconds = _newDeliveryDelay;
            SystemUnderTest.Update(queue, config);
        }

        [Test]
        public void TheDeliveryDelayIsUpdatedWithTheNewValue()
        {
            Assert.AreEqual(_newDeliveryDelay, queue.DeliveryDelay);
        }
    }
}
