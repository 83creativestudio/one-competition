namespace OneCompetitions.Application.Jobs;

public interface IPlatformJobProcessor
{
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}
