namespace OneCompetitions.Application.Locking;

public interface IDistributedLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan lifetime, CancellationToken cancellationToken);
}
