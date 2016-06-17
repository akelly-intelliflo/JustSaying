using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            queue = SystemUnderTest.EnsureQueueAndErrorQueueExists(GetQueueConfig());
            SystemUnderTest.Delete(queue);
        }

        [Test]
        public async Task TheErrorQueueIsDeleted()
        {
            await Patiently.AssertThatAsync(
                () => queue.ErrorQueue == null);
        }
    }
}