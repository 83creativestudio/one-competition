using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OneCompetitions.Application.Auditing;
using OneCompetitions.Application.Auth;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Domain.Tenants;
using OneCompetitions.Infrastructure.Identity;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Infrastructure.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IConfiguration configuration,
    IAuditLogger auditLogger) : IAuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAccessException("Email verification is required.");
        }

        var token = await IssueAsync(user, ipAddress, userAgent, cancellationToken);
        var firstTenant = token.Tenants.FirstOrDefault();
        if (firstTenant is not null)
        {
            await auditLogger.RecordAsync(
                new AuditRecord(firstTenant.TenantId, user.Id, "User", "auth.login", "User", user.Id.ToString(), null, Guid.NewGuid().ToString("N")),
                cancellationToken);
        }

        return token;
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var session = await dbContext.UserSessions
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.RefreshTokenHash == tokenHash, cancellationToken);

        if (session is null || session.User is null || session.RevokedAt is not null || session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh session is invalid.");
        }

        session.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueAsync(session.User, ipAddress, userAgent, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsSqlite())
            return (await dbContext.UserSessions.Where(x => x.UserId == userId).ToListAsync(cancellationToken)).OrderByDescending(x => x.CreatedAt)
                .Select(x => new SessionResponse(x.Id, x.CreatedAt, x.ExpiresAt, x.RevokedAt, x.IpAddress, x.UserAgent)).ToList();
        return await dbContext.UserSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SessionResponse(x.Id, x.CreatedAt, x.ExpiresAt, x.RevokedAt, x.IpAddress, x.UserAgent))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var tenantMemberships = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .Where(x => x.UserId == user.Id && x.Status == TenantUserStatus.Active)
            .Join(
                dbContext.Tenants.IgnoreQueryFilters(),
                x => x.TenantId,
                x => x.Id,
                (membership, tenant) => new AuthTenantResponse(tenant.Id, tenant.Name, tenant.Slug, membership.Role.ToString()))
            .ToListAsync(cancellationToken);

        var expiresAt = DateTimeOffset.UtcNow.Add(AccessTokenLifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (tenantMemberships.Count == 1)
        {
            claims.Add(new Claim("tenant_id", tenantMemberships[0].TenantId.ToString()));
            claims.Add(new Claim("tenant_slug", tenantMemberships[0].Slug));
        }

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        dbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenHash = Hash(refreshToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(RefreshTokenLifetime),
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSigningKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "one-competitions",
            audience: configuration["Jwt:Audience"] ?? "one-competitions-dashboard",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            refreshToken,
            expiresAt,
            user.Id,
            user.Email ?? string.Empty,
            roles.ToArray(),
            tenantMemberships);
    }

    private string GetSigningKey()
    {
        var key = configuration["JWT_SIGNING_KEY"]
            ?? configuration["Jwt:SigningKey"]
            ?? "development-only-signing-key-change-before-production";
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException("JWT_SIGNING_KEY must be configured and at least 32 characters long.");
        }

        return key;
    }

    public static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
