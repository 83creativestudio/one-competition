using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OneCompetitions.Infrastructure.Tenancy;

namespace OneCompetitions.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=one_competitions;Username=one;Password=one_dev_password";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new TenantContext());
    }
}
