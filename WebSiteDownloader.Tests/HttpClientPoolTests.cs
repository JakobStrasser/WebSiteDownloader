
using RecursiveWebDownloader;

namespace WebSiteDownloader.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task HttpClientPoolShouldGetAHttpClient()
        {
            if (!HttpClientPool.Initialized)
                HttpClientPool.Initialize(1);
            var client = await HttpClientPool.GetClientAsync();
            Assert.That(client.GetType().Equals(typeof(HttpClient)));
            HttpClientPool.ReleaseClient(client);
        }

        [Test]
        public async Task HttpClientPoolCancellationToken()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                Thread.Sleep(100);
                cts.Cancel();
            });

            if (!HttpClientPool.Initialized)
                HttpClientPool.Initialize(1);
            var client1 = await HttpClientPool.GetClientAsync();
            Assert.ThrowsAsync<OperationCanceledException>(async Task () => await HttpClientPool.GetClientAsync(cts.Token));
            HttpClientPool.ReleaseClient(client1);
        }


    }
}