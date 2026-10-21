using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneCompetitions.Contracts.Auth;
using OneCompetitions.Contracts.Competitions;
using OneCompetitions.Infrastructure.Persistence;

namespace OneCompetitions.Api.IntegrationTests;

public sealed class AccountManagementTests
{
    private const string Password = "DevelopmentOnly!ChangeMe123";

    [Fact]
    public async Task Registration_requires_verification_and_cannot_grant_admin_access_or_reuse_a_tenant()
    {
        using var factory = new TestApplicationFactory();
        await factory.SeedAsync();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new {
            displayName = "New Customer", organisationName = "New Organisation", email = "new@example.test", password = Password,
            role = "Admin", tenantId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("new@example.test", Password))).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == "new@example.test");
            Assert.False(user.EmailConfirmed);
            Assert.False(await db.UserRoles.AnyAsync(x => x.UserId == user.Id));
            var membership = await db.TenantUsers.IgnoreQueryFilters().SingleAsync(x => x.UserId == user.Id);
            var tenant = await db.Tenants.SingleAsync(x => x.Id == membership.TenantId);
            Assert.Equal("New Organisation", tenant.Name);
            var message = await db.NotificationMessages.IgnoreQueryFilters().SingleAsync(x => x.Recipient == user.Email);
            var url = new Uri(message.BodyText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last());
            var parameters = QueryHelpers.ParseQuery(url.Query);
            var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(parameters["email"]!, parameters["token"]!));
            verify.EnsureSuccessStatusCode();
        }
        var auth = await Login(client, "new@example.test");
        Assert.Empty(auth.Roles);
        Assert.Single(auth.Tenants);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/platform/users")).StatusCode);
        var competition = await client.PostAsJsonAsync("/api/competitions", Competition("My giveaway"));
        competition.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Admin_can_create_edit_suspend_reactivate_and_delete_a_customer()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var admin = factory.CreateClient(); await Login(admin, "admin@onecompetitions.local");
        var created = await Create(admin, "customer@example.test", "Customer");
        var customer = factory.CreateClient(); var auth = await Login(customer, created.Email);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/platform/users")).StatusCode);
        var update = await admin.PatchAsJsonAsync($"/api/platform/users/{created.Id}", new UpdateAccountRequest("Updated Customer", "edited@example.test", "Customer", false));
        update.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/tenants/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/auth/login", new LoginRequest("edited@example.test", Password))).StatusCode);
        (await admin.PatchAsJsonAsync($"/api/platform/users/{created.Id}", new UpdateAccountRequest("Updated Customer", "edited@example.test", "Customer", true))).EnsureSuccessStatusCode();
        await Login(customer, "edited@example.test");
        (await admin.DeleteAsync($"/api/platform/users/{created.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/tenants/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/auth/login", new LoginRequest("edited@example.test", Password))).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.NotNull((await db.Users.SingleAsync(x => x.Id == created.Id)).DeletedAt);
        Assert.True(await db.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "account.deleted" && x.EntityId == created.Id.ToString()));
    }

    [Fact]
    public async Task Customers_are_isolated_and_only_admin_can_switch_to_another_workspace()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var admin = factory.CreateClient(); await Login(admin, "admin@onecompetitions.local");
        var first = await Create(admin, "first@example.test", "Customer");
        var second = await Create(admin, "second@example.test", "Customer");
        var firstClient = factory.CreateClient(); await Login(firstClient, first.Email);
        var create = await firstClient.PostAsJsonAsync("/api/competitions", Competition("Private giveaway")); create.EnsureSuccessStatusCode();
        var competition = (await create.Content.ReadFromJsonAsync<CompetitionResponse>())!;
        var secondClient = factory.CreateClient(); await Login(secondClient, second.Email);
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync($"/api/competitions/{competition.Id}")).StatusCode);
        Assert.Empty((await secondClient.GetFromJsonAsync<List<CompetitionResponse>>("/api/competitions"))!);
        secondClient.DefaultRequestHeaders.Add("X-One-Tenant", first.Workspaces[0].Slug);
        Assert.Equal(HttpStatusCode.Unauthorized, (await secondClient.GetAsync("/api/competitions")).StatusCode);
        secondClient.DefaultRequestHeaders.Remove("X-One-Tenant");
        Assert.Equal(HttpStatusCode.Forbidden, (await secondClient.PostAsJsonAsync("/api/platform/users", new CreateAccountRequest("Attack", "attack@example.test", Password, "Admin", "Attack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await secondClient.PatchAsJsonAsync($"/api/platform/users/{second.Id}", new UpdateAccountRequest("Attack", second.Email, "Admin", true))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await secondClient.DeleteAsync($"/api/platform/users/{first.Id}")).StatusCode);
        admin.DefaultRequestHeaders.Add("X-One-Tenant", first.Workspaces[0].Slug);
        (await admin.GetAsync($"/api/competitions/{competition.Id}")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Admin_role_changes_revoke_old_tokens_and_admin_cannot_remove_own_access()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var admin = factory.CreateClient(); var own = await Login(admin, "admin@onecompetitions.local");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/platform/users/{own.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/platform/users/{own.UserId}", new UpdateAccountRequest("Admin", own.Email, "Customer", true))).StatusCode);
        var created = await Create(admin, "newadmin@example.test", "Admin");
        var newAdmin = factory.CreateClient(); await Login(newAdmin, created.Email);
        (await newAdmin.GetAsync("/api/platform/users")).EnsureSuccessStatusCode();
        (await admin.PatchAsJsonAsync($"/api/platform/users/{created.Id}", new UpdateAccountRequest(created.DisplayName, created.Email, "Customer", true))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await newAdmin.GetAsync("/api/platform/users")).StatusCode);
        var demoted = await Login(newAdmin, created.Email);
        Assert.Empty(demoted.Roles);
        Assert.Equal(HttpStatusCode.Forbidden, (await newAdmin.GetAsync("/api/platform/users")).StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_does_not_leave_a_partial_workspace_and_list_is_paginated()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var admin = factory.CreateClient(); await Login(admin, "admin@onecompetitions.local");
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Tenants.CountAsync();
        var invalid = await admin.PostAsJsonAsync("/api/platform/users", new CreateAccountRequest("Invalid User", "invalid@example.test", "aaaaaaaaaaaa", "Customer", "Invalid Co"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(count, await db.Tenants.CountAsync());
        Assert.False(await db.Users.AnyAsync(x => x.Email == "invalid@example.test"));
        var page = await admin.GetFromJsonAsync<AccountPageResponse>("/api/platform/users?pageSize=1");
        Assert.Single(page!.Items);
        Assert.True(page.Total > 1);
    }

    [Fact]
    public async Task Admin_can_reset_password_and_unlock_login_without_granting_customer_admin_access()
    {
        using var factory = new TestApplicationFactory(); await factory.SeedAsync();
        var admin = factory.CreateClient(); await Login(admin, "admin@onecompetitions.local");
        var account = await Create(admin, "support@example.test", "Customer");
        var customer = factory.CreateClient(); var session = await Login(customer, account.Email);
        var reset = new UpdateAccountRequest(account.DisplayName, account.Email, "Customer", true, "NewSupport!Password123", true);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PatchAsJsonAsync($"/api/platform/users/{account.Id}", reset)).StatusCode);
        var invalid = reset with { Email = "should-not-change@example.test", NewPassword = "aaaaaaaaaaaa" };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"/api/platform/users/{account.Id}", invalid)).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(x => x.Id == account.Id);
            Assert.Equal(account.Email, user.Email);
            user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
            user.AccessFailedCount = 5;
            await db.SaveChangesAsync();
        }
        (await admin.PatchAsJsonAsync($"/api/platform/users/{account.Id}", reset)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/tenants/current")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.PostAsJsonAsync("/api/auth/login", new LoginRequest(account.Email, Password))).StatusCode);
        var login = await customer.PostAsJsonAsync("/api/auth/login", new LoginRequest(account.Email, reset.NewPassword!));
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Empty(auth.Roles);
        using var auditScope = factory.Services.CreateScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await auditDb.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "account.password.reset" && x.EntityId == account.Id.ToString()));
        Assert.True(await auditDb.AuditEvents.IgnoreQueryFilters().AnyAsync(x => x.Action == "account.login.unlocked" && x.EntityId == account.Id.ToString()));
    }

    private static async Task<AuthResponse> Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    private static async Task<AccountResponse> Create(HttpClient admin, string email, string role)
    {
        var response = await admin.PostAsJsonAsync("/api/platform/users", new CreateAccountRequest("Test User", email, Password, role, "Test Organisation"));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }

    private static CreateCompetitionRequest Competition(string name) => new(name, "giveaway", null,
        DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(1), null, 1, 1, 0, false);
}
