using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OneCompetitions.Application.SocialAuth;
using OneCompetitions.Application.Tenants;
using OneCompetitions.Contracts.SocialAuth;
using OneCompetitions.Domain.Competitions;
using OneCompetitions.Domain.Auditing;
using OneCompetitions.Domain.Participants;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class SocialAuthService(
    AppDbContext dbContext,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    ISocialAuthProviderRegistry providers,
    SecretProtector protector,
    IConfiguration configuration,
    IHostEnvironment environment)
    : TenantScopedServiceBase(dbContext, tenantContext, userManager), ISocialAuthService
{
    private static readonly HashSet<string> SupportedProviderNames =
        new(["Google", "Apple", "Facebook", "X", "TikTok"], StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<SocialAuthProviderResponse>> ProvidersAsync(string competitionSlug, CancellationToken cancellationToken)
    {
        await EnsureSocialLoginEnabledAsync(cancellationToken);
        var competition = await GetPublicCompetitionAsync(competitionSlug, cancellationToken);
        var allowed = ParseAllowedProviders(competition.AllowedParticipantAuthProvidersJson);
        return providers.All.Where(x => allowed.Contains(x.Name))
            .Select(ToProviderResponse).ToList();
    }

    public async Task<SocialAuthStartResult> StartAsync(string competitionSlug, string provider, string returnUrl, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        await EnsureSocialLoginEnabledAsync(cancellationToken);
        var competition = await GetPublicCompetitionAsync(competitionSlug, cancellationToken);
        var adapter = providers.Get(provider);
        if (!adapter.IsConfigured) throw new InvalidOperationException($"{adapter.DisplayName} login is not configured.");
        if (!ParseAllowedProviders(competition.AllowedParticipantAuthProvidersJson).Contains(adapter.Name))
            throw new UnauthorizedAccessException("This login method is not enabled for the competition.");

        var tenant = await DbContext.Tenants.IgnoreQueryFilters().SingleAsync(x => x.Id == TenantContext.TenantId, cancellationToken);
        var validatedReturnUrl = await ValidateReturnUrlAsync(returnUrl, tenant, cancellationToken);
        var state = RandomToken(48);
        var nonce = RandomToken(32);
        var verifier = RandomToken(64);
        var challenge = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(verifier)));
        var redirectUri = CallbackUri(adapter.Name).ToString();

        DbContext.AuthTransactions.Add(new AuthTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = TenantContext.TenantId,
            CompetitionId = competition.Id,
            Provider = adapter.Name,
            StateHash = Hash(state),
            PkceChallenge = challenge,
            PkceVerifierCiphertext = protector.Protect(verifier),
            NonceHash = Hash(nonce),
            NonceCiphertext = protector.Protect(nonce),
            ReturnUrl = validatedReturnUrl.ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await DbContext.SaveChangesAsync(cancellationToken);

        return new SocialAuthStartResult(adapter.CreateAuthorizationUri(
            new SocialAuthorizationRequest(redirectUri, state, nonce, challenge)));
    }

    public async Task<SocialAuthCallbackResult> CompleteProviderCallbackAsync(
        string provider, string? code, string state, string? error, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state)) throw new UnauthorizedAccessException("The social authentication state is missing.");
        var transaction = await DbContext.AuthTransactions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.StateHash == Hash(state), cancellationToken)
            ?? throw new UnauthorizedAccessException("The social authentication transaction is invalid.");
        if (!transaction.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("The social authentication provider does not match the transaction.");
        if (transaction.ExpiresAt <= DateTimeOffset.UtcNow || transaction.CompletedAt is not null)
            throw new UnauthorizedAccessException("The social authentication transaction has expired or was already completed.");
        if (!string.IsNullOrWhiteSpace(error))
        {
            transaction.CompletedAt = DateTimeOffset.UtcNow;
            transaction.UsedAt = transaction.CompletedAt;
            await DbContext.SaveChangesAsync(cancellationToken);
            return new SocialAuthCallbackResult(AppendQuery(new Uri(transaction.ReturnUrl), "social_error", "access_denied"));
        }
        if (string.IsNullOrWhiteSpace(code)) throw new UnauthorizedAccessException("The social provider did not return an authorization code.");

        var adapter = providers.Get(transaction.Provider);
        if (!adapter.IsConfigured) throw new InvalidOperationException("The social authentication provider is not configured.");
        var nonce = protector.Unprotect(transaction.NonceCiphertext);
        if (!FixedEquals(transaction.NonceHash, Hash(nonce))) throw new UnauthorizedAccessException("The social authentication nonce is invalid.");
        var profile = await adapter.ExchangeAsync(new SocialTokenExchangeRequest(
            code, CallbackUri(adapter.Name).ToString(), protector.Unprotect(transaction.PkceVerifierCiphertext), nonce), cancellationToken);
        if (string.IsNullOrWhiteSpace(profile.Subject)) throw new UnauthorizedAccessException("The social provider identity is invalid.");

        await using var databaseTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var subjectHash = Hash(profile.Subject);
        var identity = await DbContext.ParticipantIdentities.IgnoreQueryFilters()
            .Include(x => x.Participant)
            .SingleOrDefaultAsync(x => x.TenantId == transaction.TenantId && x.Provider == adapter.Name && x.ProviderSubjectHash == subjectHash, cancellationToken);

        Participant participant;
        if (identity is not null)
        {
            participant = identity.Participant;
        }
        else
        {
            var email = NormalizeEmail(profile.Email);
            participant = profile.EmailVerified && email is not null
                ? await DbContext.Participants.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == transaction.TenantId && x.PrimaryEmail == email, cancellationToken)
                    ?? CreateParticipant(transaction.TenantId, profile, now)
                : CreateParticipant(transaction.TenantId, profile, now);
            if (DbContext.Entry(participant).State == EntityState.Detached) DbContext.Participants.Add(participant);
            identity = new ParticipantIdentity
            {
                Id = Guid.NewGuid(), TenantId = transaction.TenantId, ParticipantId = participant.Id,
                Provider = adapter.Name, ProviderSubjectHash = subjectHash, ConnectedAt = now, CreatedAt = now
            };
            DbContext.ParticipantIdentities.Add(identity);
        }

        UpdateParticipant(participant, profile, now);
        identity.ProviderEmail = NormalizeEmail(profile.Email);
        identity.ProviderSubjectCiphertext = protector.Protect(profile.Subject);
        identity.ProviderUserName = profile.UserName;
        identity.IsVerified = true;
        identity.EmailVerified = profile.EmailVerified;
        identity.AccessTokenCiphertext = protector.Protect(profile.AccessToken);
        identity.RefreshTokenCiphertext = string.IsNullOrWhiteSpace(profile.RefreshToken) ? identity.RefreshTokenCiphertext : protector.Protect(profile.RefreshToken);
        identity.GrantedScopes = string.Join(' ', profile.GrantedScopes);
        identity.TokenExpiresAt = profile.TokenExpiresAt;
        identity.LastUsedAt = now;

        var completionCode = RandomToken(48);
        DbContext.SocialAuthCompletions.Add(new SocialAuthCompletion
        {
            Id = Guid.NewGuid(), AuthTransactionId = transaction.Id, TenantId = transaction.TenantId,
            CompetitionId = transaction.CompetitionId, ParticipantId = participant.Id, ParticipantIdentityId = identity.Id,
            CodeHash = Hash(completionCode), ReturnUrl = transaction.ReturnUrl, ExpiresAt = now.AddMinutes(2), CreatedAt = now
        });
        transaction.CompletedAt = now;
        transaction.UsedAt = now;
        DbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), TenantId = transaction.TenantId, ActorType = "Participant", Action = "participant.social_identity.connected",
            EntityType = "ParticipantIdentity", EntityId = identity.Id.ToString(), CompetitionId = transaction.CompetitionId,
            MetadataJson = JsonSerializer.Serialize(new { provider = adapter.Name }), OccurredAt = now, CorrelationId = Guid.NewGuid().ToString("N")
        });
        await DbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        var returnUri = AppendQuery(new Uri(transaction.ReturnUrl), "social_code", completionCode);
        returnUri = AppendQuery(returnUri, "social_provider", adapter.Name);
        return new SocialAuthCallbackResult(returnUri);
    }

    public async Task<SocialAuthSessionResponse> ExchangeCompletionAsync(CompleteSocialAuthRequest request, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        if (string.IsNullOrWhiteSpace(request.Code)) throw new UnauthorizedAccessException("The social completion code is missing.");
        var completion = await DbContext.SocialAuthCompletions.SingleOrDefaultAsync(x => x.CodeHash == Hash(request.Code), cancellationToken)
            ?? throw new UnauthorizedAccessException("The social completion code is invalid.");
        if (completion.ExpiresAt <= DateTimeOffset.UtcNow || completion.UsedAt is not null)
            throw new UnauthorizedAccessException("The social completion code has expired or was already used.");

        var identity = await DbContext.ParticipantIdentities.Include(x => x.Participant)
            .SingleAsync(x => x.Id == completion.ParticipantIdentityId, cancellationToken);
        var token = RandomToken(48);
        var now = DateTimeOffset.UtcNow;
        DbContext.ParticipantSessions.Add(new ParticipantSession
        {
            Id = Guid.NewGuid(), TenantId = completion.TenantId, CompetitionId = completion.CompetitionId,
            ParticipantId = completion.ParticipantId, ParticipantIdentityId = completion.ParticipantIdentityId,
            TokenHash = Hash(token), ExpiresAt = now.AddMinutes(30), CreatedAt = now
        });
        completion.UsedAt = now;
        await DbContext.SaveChangesAsync(cancellationToken);

        var actions = await PublicRequirementsAsync(completion.CompetitionId, completion.ParticipantId, cancellationToken);
        return new SocialAuthSessionResponse(identity.Provider, identity.ProviderEmail,
            DisplayName(identity.Participant), identity.ProviderUserName, actions, token);
    }

    public async Task<IReadOnlyList<SocialActionRequirementResponse>> ListRequirementsAsync(Guid userId, Guid competitionId, CancellationToken cancellationToken)
    {
        await EnsureTenantMemberAsync(userId, cancellationToken);
        await EnsureCompetitionAsync(competitionId, cancellationToken);
        return (await DbContext.CompetitionSocialActionRequirements.Where(x => x.CompetitionId == competitionId)
            .OrderBy(x => x.Provider).ThenBy(x => x.ActionType).ToListAsync(cancellationToken)).Select(x => ToRequirementResponse(x)).ToList();
    }

    public async Task<SocialActionRequirementResponse> AddRequirementAsync(Guid userId, Guid competitionId, UpsertSocialActionRequirementRequest request, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        await EnsureSocialLoginEnabledAsync(cancellationToken);
        var competition = await EnsureCompetitionAsync(competitionId, cancellationToken);
        if (competition.PublishedAt is not null) throw new InvalidOperationException("Published social participation requirements are immutable.");
        var provider = providers.Get(request.Provider);
        var action = request.ActionType.Trim();
        var target = request.TargetReference.Trim();
        if (action.Length is < 2 or > 80 || target.Length is < 1 or > 500) throw new InvalidOperationException("Social action and target are required.");
        if (request.IsRequired && !provider.SupportedActions.Contains(action))
            throw new InvalidOperationException($"{provider.DisplayName} does not support automated verification for '{action}'. It may only be added as an optional instruction.");
        var requirement = new CompetitionSocialActionRequirement
        {
            Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, CompetitionId = competitionId,
            Provider = provider.Name, ActionType = action, TargetReference = target,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsRequired = request.IsRequired, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        DbContext.CompetitionSocialActionRequirements.Add(requirement);
        await DbContext.SaveChangesAsync(cancellationToken);
        return ToRequirementResponse(requirement);
    }

    public async Task DeleteRequirementAsync(Guid userId, Guid competitionId, Guid requirementId, CancellationToken cancellationToken)
    {
        await EnsureTenantManagerAsync(userId, cancellationToken);
        var competition = await EnsureCompetitionAsync(competitionId, cancellationToken);
        if (competition.PublishedAt is not null) throw new InvalidOperationException("Published social participation requirements are immutable.");
        var requirement = await DbContext.CompetitionSocialActionRequirements
            .SingleOrDefaultAsync(x => x.CompetitionId == competitionId && x.Id == requirementId, cancellationToken)
            ?? throw new InvalidOperationException("Social action requirement was not found.");
        DbContext.CompetitionSocialActionRequirements.Remove(requirement);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SocialActionVerificationResponse> VerifyActionAsync(Guid requirementId, string participantSessionToken, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved || string.IsNullOrWhiteSpace(participantSessionToken))
            throw new UnauthorizedAccessException("A participant session is required.");
        var session = await DbContext.ParticipantSessions.SingleOrDefaultAsync(x => x.TokenHash == Hash(participantSessionToken), cancellationToken)
            ?? throw new UnauthorizedAccessException("The participant session is invalid.");
        if (session.ExpiresAt <= DateTimeOffset.UtcNow || session.ConsumedAt is not null)
            throw new UnauthorizedAccessException("The participant session has expired.");
        var requirement = await DbContext.CompetitionSocialActionRequirements
            .SingleOrDefaultAsync(x => x.Id == requirementId && x.CompetitionId == session.CompetitionId, cancellationToken)
            ?? throw new InvalidOperationException("Social action requirement was not found.");
        var identity = await DbContext.ParticipantIdentities.SingleAsync(x => x.Id == session.ParticipantIdentityId, cancellationToken);
        if (!identity.Provider.Equals(requirement.Provider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Connect a {requirement.Provider} account to verify this action.");
        var provider = providers.Get(identity.Provider);
        if (!provider.SupportedActions.Contains(requirement.ActionType))
            return new SocialActionVerificationResponse(requirement.Id, "Unsupported", null, null);
        if (string.IsNullOrWhiteSpace(identity.AccessTokenCiphertext)) throw new UnauthorizedAccessException("The provider session is unavailable.");

        var result = await provider.VerifyActionAsync(new SocialActionVerificationRequest(
            protector.Unprotect(identity.ProviderSubjectCiphertext), protector.Unprotect(identity.AccessTokenCiphertext), requirement.ActionType, requirement.TargetReference), cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var verification = await DbContext.ParticipantSocialActionVerifications
            .SingleOrDefaultAsync(x => x.RequirementId == requirement.Id && x.ParticipantId == session.ParticipantId, cancellationToken);
        if (verification is null)
        {
            verification = new ParticipantSocialActionVerification
            {
                Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, CompetitionId = session.CompetitionId,
                RequirementId = requirement.Id, ParticipantId = session.ParticipantId, ParticipantIdentityId = identity.Id,
                CreatedAt = now
            };
            DbContext.ParticipantSocialActionVerifications.Add(verification);
        }
        verification.Status = result.Status;
        verification.EvidenceJson = result.EvidenceJson;
        verification.VerifiedAt = result.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase) ? now : null;
        verification.UpdatedAt = now;
        DbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), TenantId = TenantContext.TenantId, ActorType = "Participant", Action = "participant.social_action.checked",
            EntityType = "CompetitionSocialActionRequirement", EntityId = requirement.Id.ToString(), CompetitionId = session.CompetitionId,
            MetadataJson = JsonSerializer.Serialize(new { provider = provider.Name, action = requirement.ActionType, result = verification.Status }),
            OccurredAt = now, CorrelationId = Guid.NewGuid().ToString("N")
        });
        await DbContext.SaveChangesAsync(cancellationToken);
        return new SocialActionVerificationResponse(requirement.Id, verification.Status, verification.EvidenceJson, verification.VerifiedAt);
    }

    private async Task<Competition> GetPublicCompetitionAsync(string slug, CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        var competition = await DbContext.Competitions.SingleOrDefaultAsync(x => x.Slug == slug.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");
        if (competition.Status is not (CompetitionStatus.Live or CompetitionStatus.Scheduled or CompetitionStatus.Paused))
            throw new InvalidOperationException("Competition is not available for participant authentication.");
        return competition;
    }

    private async Task<Competition> EnsureCompetitionAsync(Guid competitionId, CancellationToken cancellationToken)
    {
        return await DbContext.Competitions.SingleOrDefaultAsync(x => x.Id == competitionId, cancellationToken)
            ?? throw new InvalidOperationException("Competition was not found.");
    }

    private async Task EnsureSocialLoginEnabledAsync(CancellationToken cancellationToken)
    {
        if (!TenantContext.IsResolved) throw new UnauthorizedAccessException("Tenant context is required.");
        var flag = await DbContext.FeatureFlags.SingleOrDefaultAsync(x => x.Code == "SocialLogin", cancellationToken);
        if (flag is null) throw new InvalidOperationException("Social login is not enabled on the platform.");
        var tenantOverride = await DbContext.TenantFeatureOverrides
            .SingleOrDefaultAsync(x => x.TenantId == TenantContext.TenantId && x.FeatureFlagId == flag.Id, cancellationToken);
        if (!(tenantOverride?.IsEnabled ?? flag.IsEnabled)) throw new InvalidOperationException("Social login is not enabled for this organisation.");
    }

    private async Task<Uri> ValidateReturnUrlAsync(string returnUrl, Tenant tenant, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new UnauthorizedAccessException("The return URL is invalid.");
        var developmentHttp = (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            && (uri.IsLoopback || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !(developmentHttp && uri.Scheme == Uri.UriSchemeHttp))
            throw new UnauthorizedAccessException("The return URL must use HTTPS.");

        var hostname = uri.IdnHost.ToLowerInvariant();
        var verifiedDomain = await DbContext.TenantDomains.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == tenant.Id && x.Hostname == hostname && x.DeletedAt == null && x.Status == TenantDomainStatus.Active, cancellationToken);
        var baseDomain = (configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local").Trim().Trim('.').ToLowerInvariant();
        var platformPath = hostname == baseDomain && uri.AbsolutePath.StartsWith($"/c/{tenant.Slug}/", StringComparison.OrdinalIgnoreCase);
        var platformSubdomain = hostname == $"{tenant.Slug}.{baseDomain}";
        if (!verifiedDomain && !platformPath && !platformSubdomain && !developmentHttp)
            throw new UnauthorizedAccessException("The return URL is not a verified domain for this organisation.");
        return uri;
    }

    private Uri CallbackUri(string provider)
    {
        var configured = configuration["PLATFORM_AUTH_DOMAIN"] ?? $"auth.{configuration["PLATFORM_BASE_DOMAIN"] ?? "competitions.local"}";
        var baseUri = configured.Contains("://", StringComparison.Ordinal)
            ? new Uri(configured.TrimEnd('/'))
            : new Uri($"{((environment.IsDevelopment() || environment.IsEnvironment("Testing")) ? "http" : "https")}://{configured.TrimEnd('/')}");
        return new Uri(baseUri, $"/api/participant-auth/{Uri.EscapeDataString(provider.ToLowerInvariant())}/callback");
    }

    private async Task<IReadOnlyList<SocialActionRequirementResponse>> PublicRequirementsAsync(Guid competitionId, Guid participantId, CancellationToken cancellationToken)
    {
        var requirements = await DbContext.CompetitionSocialActionRequirements.Where(x => x.CompetitionId == competitionId).ToListAsync(cancellationToken);
        var states = await DbContext.ParticipantSocialActionVerifications.Where(x => x.CompetitionId == competitionId && x.ParticipantId == participantId)
            .ToDictionaryAsync(x => x.RequirementId, x => x.Status, cancellationToken);
        return requirements.Select(x => ToRequirementResponse(x, states.GetValueOrDefault(x.Id))).ToList();
    }

    private SocialActionRequirementResponse ToRequirementResponse(CompetitionSocialActionRequirement requirement, string? status = null)
    {
        var provider = providers.Get(requirement.Provider);
        return new SocialActionRequirementResponse(requirement.Id, provider.Name, requirement.ActionType, requirement.TargetReference,
            requirement.Description, requirement.IsRequired, provider.SupportedActions.Contains(requirement.ActionType), status);
    }

    private static SocialAuthProviderResponse ToProviderResponse(ISocialAuthProvider provider) =>
        new(provider.Name, provider.DisplayName, provider.IsConfigured, provider.SupportedActions.Order().ToList());

    private static HashSet<string> ParseAllowedProviders(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json)?.Where(x => x.Equals("Email", StringComparison.OrdinalIgnoreCase) || SupportedProviderNames.Contains(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(["Email"], StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(["Email"], StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Participant CreateParticipant(Guid tenantId, SocialIdentityProfile profile, DateTimeOffset now)
    {
        var (firstName, lastName) = SplitName(profile.DisplayName);
        return new Participant
        {
            Id = Guid.NewGuid(), TenantId = tenantId, PrimaryEmail = profile.EmailVerified ? NormalizeEmail(profile.Email) : null,
            FirstName = firstName, LastName = lastName, Status = ParticipantStatus.Active,
            PreferredLanguage = "en", CreatedAt = now, UpdatedAt = now
        };
    }

    private static void UpdateParticipant(Participant participant, SocialIdentityProfile profile, DateTimeOffset now)
    {
        var (firstName, lastName) = SplitName(profile.DisplayName);
        if (profile.EmailVerified) participant.PrimaryEmail ??= NormalizeEmail(profile.Email);
        participant.FirstName ??= firstName;
        participant.LastName ??= lastName;
        participant.UpdatedAt = now;
    }

    private static (string? FirstName, string? LastName) SplitName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return (null, null);
        var parts = displayName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static string? NormalizeEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static string DisplayName(Participant participant) => string.Join(' ', new[] { participant.FirstName, participant.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string RandomToken(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool FixedEquals(string first, string second) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));

    private static Uri AppendQuery(Uri uri, string name, string value)
    {
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return new Uri($"{uri.GetLeftPart(UriPartial.Path)}{uri.Query}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}{uri.Fragment}");
    }
}
