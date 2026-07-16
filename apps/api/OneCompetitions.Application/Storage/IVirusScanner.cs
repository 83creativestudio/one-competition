namespace OneCompetitions.Application.Storage;

public interface IVirusScanner
{
    Task<VirusScanResult> ScanAsync(Stream content, string filename, CancellationToken cancellationToken);
}

public sealed record VirusScanResult(bool IsClean, string? ThreatName = null);
