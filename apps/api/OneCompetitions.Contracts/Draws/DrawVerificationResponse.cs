namespace OneCompetitions.Contracts.Draws;

public sealed record DrawVerificationResponse(
    string DrawReference,
    string Status,
    string CompetitionName,
    string Organiser,
    DateTimeOffset? DrawnAt,
    int EligibleEntryCount,
    int ExcludedEntryCount,
    string EntryPoolHash,
    string ConfigurationHash,
    string AlgorithmVersion,
    bool CertificateValid,
    string? CertificateSha256Hash);
