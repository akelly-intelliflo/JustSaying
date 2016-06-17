using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class NullCache<T> : IRegionResourceCache<T>
    {
        public T TryGetFromCache(string region, string key)
        {
            return default(T);
        }

        public void AddToCache(string region, string key, T value)
        {
        }
    }
}