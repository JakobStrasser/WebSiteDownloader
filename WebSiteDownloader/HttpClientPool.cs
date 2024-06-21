using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RecursiveWebDownloader;
public static class HttpClientPool
{
    private static ConcurrentBag<HttpClient>? _clients;
    private static SemaphoreSlim? _semaphore;
    private static bool _disposed = false;
    public static bool Initialized { get; set; } = false;
    private static readonly object _lock = new object();

    public static void Initialize(int poolSize)
    {
        if (Initialized)
        {
            throw new InvalidOperationException("HttpClientPool is already initialized.");
        }

        if (poolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be greater than zero.");
        }

        lock (_lock)
        {
            if (Initialized) return; 

            _clients = new ConcurrentBag<HttpClient>();
            _semaphore = new SemaphoreSlim(poolSize, poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                _clients.Add(new HttpClient());
            }

            Initialized = true;
        }
    }

    public static async Task<HttpClient> GetClientAsync(CancellationToken cancellationToken = default)
    {
        if (!Initialized || _semaphore is null || _clients is null)
        {
            throw new InvalidOperationException("HttpClientPool is not initialized. Call Initialize first.");
        }

        await _semaphore.WaitAsync(cancellationToken);

        if (_disposed)
        {
            _semaphore.Release();
            throw new ObjectDisposedException(nameof(HttpClientPool));
        }

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _semaphore.Release();
                throw new OperationCanceledException(cancellationToken);
            }

            if (_clients.TryTake(out var client))
            {
                return client;
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    public static void ReleaseClient(HttpClient client)
    {
        if (!Initialized || _semaphore is null || _clients is null)
        {
            throw new InvalidOperationException("HttpClientPool is not initialized. Call Initialize first.");
        }
        if (client == null) throw new ArgumentNullException(nameof(client));

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpClientPool));
        }

        _clients.Add(client);
        _semaphore.Release();
    }

    public static void Dispose()
    {
        if (!Initialized || _semaphore is null || _clients is null)
        {
            throw new InvalidOperationException("HttpClientPool is not initialized. Call Initialize first.");
        }
        if (!_disposed)
        {
            _disposed = true;
            _semaphore.Dispose();

            while (_clients.TryTake(out var client))
            {
                client.Dispose();
            }
        }
    }
}
