using System.ComponentModel.DataAnnotations;

namespace OneCompetitions.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    string? TwoFactorCode = null);
