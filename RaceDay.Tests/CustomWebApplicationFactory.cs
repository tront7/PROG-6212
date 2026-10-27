using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>
/// Spins up the RaceDay API in-memory for integration tests. SQL Server is swapped
/// for EF Core's InMemory provider and Azure Blob Storage for a fake, so the tests
/// run on any machine (and in CI) with no external dependencies.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // One database name and root per factory, so every request shares the same data.
    private readonly string _dbName = $"RaceDayTestDb_{Guid.NewGuid()}";
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                                     d.ServiceType == typeof(DbContextOptions) ||
                                     d.ServiceType == typeof(IDbContextOptionsConfiguration<RaceDayDbContext>))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, _dbRoot));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
