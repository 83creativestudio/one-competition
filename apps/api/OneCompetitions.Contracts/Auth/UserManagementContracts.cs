using System.ComponentModel.DataAnnotations;

namespace OneCompetitions.Contracts.Auth;

public sealed record RegisterCustomerRequest(
    [Required, StringLength(160, MinimumLength = 2)] string DisplayName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password,
    [Required, StringLength(200, MinimumLength = 2)] string OrganisationName);

public sealed record CreateAccountRequest(
    [Required, StringLength(160, MinimumLength = 2)] string DisplayName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password,
    [Required, RegularExpression("^(Admin|Customer)$")] string Role,
    [Required, StringLength(200, MinimumLength = 2)] string OrganisationName);

public sealed record UpdateAccountRequest(
    [Required, StringLength(160, MinimumLength = 2)] string DisplayName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, RegularExpression("^(Admin|Customer)$")] string Role,
    bool IsActive,
    [StringLength(128, MinimumLength = 12)] string? NewPassword = null,
    bool UnlockLogin = false);

public sealed record AccountWorkspaceResponse(Guid Id, string Name, string Slug);
public sealed record AccountResponse(Guid Id, string DisplayName, string Email, string Role,
    bool IsActive, bool EmailConfirmed, bool MfaEnabled, DateTimeOffset CreatedAt,
    IReadOnlyList<AccountWorkspaceResponse> Workspaces,
    bool IsLockedOut);
public sealed record AccountPageResponse(IReadOnlyList<AccountResponse> Items, int Total, int Page, int PageSize);
