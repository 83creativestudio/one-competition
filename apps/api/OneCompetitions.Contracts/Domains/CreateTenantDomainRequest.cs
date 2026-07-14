using System.ComponentModel.DataAnnotations;

namespace OneCompetitions.Contracts.Domains;

public sealed record CreateTenantDomainRequest(
    [Required] string Hostname,
    [Required] string DomainType);
