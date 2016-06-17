using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            var config = GetQueueConfig();
            config.ErrorQueueOptOut = true;
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(config);
        }

        [Then]
        public async Task ThereIsNoErrorQueue()
        {
            await Patiently.AssertThatAsync(() => queue.ErrorQueue == null);
        }
    }
}