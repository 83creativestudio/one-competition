namespace OneCompetitions.Contracts.Auth;

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record VerifyEmailRequest(string Email, string Token);
public sealed record MfaSetupRequest(string Email, string Password);
public sealed record MfaSetupResponse(string SharedKey, string AuthenticatorUri);
public sealed record MfaEnableRequest(string Email, string Password, string Code);
public sealed record MfaDisableRequest(string Password, string Code);
public sealed record AcceptInvitationRequest(Guid InvitationId, string Token, string Password, string DisplayName);
