using AccountLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AccountLedger.IntegrationTests.Infrastructure;

public sealed class LedgerApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LedgerDbContext>>();
            services.RemoveAll<LedgerDbContext>();
            services.AddDbContext<LedgerDbContext>(options => options.UseNpgsql(connectionString));
        });

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Database:Migrations:MaxAttempts"] = "1",
                ["Database:Migrations:DelaySeconds"] = "0",
                ["Api:Swagger:Enabled"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Logging:LogLevel:Default"] = "Warning"
            };

            configBuilder.AddInMemoryCollection(values);
        });
    }
}
