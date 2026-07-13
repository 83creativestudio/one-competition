using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OneCompetitions.Api.Middleware;
using OneCompetitions.Application.Auth;
using OneCompetitions.Infrastructure;
using OneCompetitions.Infrastructure.Persistence;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);

var signingKey = builder.Configuration["JWT_SIGNING_KEY"]
    ?? builder.Configuration["Jwt:SigningKey"]
    ?? "development-only-signing-key-change-before-production";

if (!builder.Environment.IsDevelopment()
    && !builder.Environment.IsEnvironment("Testing")
    && string.IsNullOrWhiteSpace(builder.Configuration["JWT_SIGNING_KEY"])
    && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SigningKey"]))
{
    throw new InvalidOperationException("JWT_SIGNING_KEY must be configured outside development and test environments.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "one-competitions",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "one-competitions-dashboard",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    context.Response.Headers["X-Auth-Error"] = context.Exception.Message;
                    return Task.CompletedTask;
                }
            };
        }
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.PlatformAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AppRoles.PlatformOwner, AppRoles.PlatformAdministrator);
    });

    options.AddPolicy(AppPolicies.TenantMember, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole(AppRoles.PlatformOwner)
            || context.User.IsInRole(AppRoles.PlatformAdministrator)
            || context.User.HasClaim(claim => claim.Type == "tenant_id"));
    });

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("one-competitions-api"))
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await DevelopmentSeeder.SeedAsync(app.Services, app.Environment, app.Configuration);
}

app.Run();

public partial class Program;
