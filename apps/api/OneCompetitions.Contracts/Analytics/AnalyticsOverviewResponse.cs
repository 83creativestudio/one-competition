namespace OneCompetitions.Contracts.Analytics;

public sealed record AnalyticsOverviewResponse(long QrScans, long UniqueQrScans, long SubmittedEntries, long ApprovedEntries, long RejectedEntries, long HighRiskEntries, decimal ConversionRate);
