using System.Collections.Concurrent;
using OneCompetitions.Application.Locking;
using StackExchange.Redis;

namespace OneCompetitions.Infrastructure.Services;

public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    private static readonly ConcurrentDictionary<string, byte> Locks = new();

    public Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        IAsyncDisposable? handle = Locks.TryAdd(resource, 0) ? new Handle(resource) : null;
        return Task.FromResult(handle);
    }

    private sealed class Handle(string resource) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() { Locks.TryRemove(resource, out _); return ValueTask.CompletedTask; }
    }
}

public sealed class RedisDistributedLockProvider(IConnectionMultiplexer redis) : IDistributedLockProvider
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        var key = $"one-competitions:lock:{resource}";
        var acquired = await redis.GetDatabase().StringSetAsync(key, token, lifetime, When.NotExists);
        return acquired ? new RedisHandle(redis.GetDatabase(), key, token) : null;
    }

    private sealed class RedisHandle(IDatabase database, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            const string script = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
            await database.ScriptEvaluateAsync(script, [key], [token]);
        }
    }
}
