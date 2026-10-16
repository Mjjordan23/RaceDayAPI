using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayAPI.Data;

namespace RaceDayAPI.Tests
{
    /// <summary>
    /// Starts the real API in memory for integration tests, but swaps SQL Server for an
    /// InMemory database so the tests need no database server (and run in GitHub Actions).
    /// Each factory gets its own empty database.
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
            });
        }
    }
}
