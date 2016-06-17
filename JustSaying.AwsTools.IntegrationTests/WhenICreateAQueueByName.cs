using System.Threading.Tasks;
using JustBehave;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(GetQueueConfig());
        }

        [Then]
        public void TheQueueISCreated()
        {
            Assert.IsTrue(queue != null);
        }

        [Then, Explicit("Extremely long running test")]
        public async Task DeadLetterQueueIsCreated()
        {
            await Patiently.AssertThatAsync(
                () => queue.ErrorQueue != null, 
                40.Seconds());
        }
    }
}