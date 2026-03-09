using AccountLedger.Application.Abstractions;
using AccountLedger.Infrastructure.Configuration;
using AccountLedger.Infrastructure.Persistence;
using AccountLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccountLedger.Infrastructure;

/// <summary>
/// Методы регистрации зависимостей инфраструктурного слоя.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости инфраструктурного слоя.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Корневая конфигурация приложения.</param>
    /// <returns>Обновленная коллекция сервисов.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is not configured.");
        }

        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(connectionString));
        var defaults = new LedgerServiceOptions();
        var attempts = int.TryParse(configuration["Infrastructure:DuplicateRead:Attempts"], out var parsedAttempts)
            ? parsedAttempts
            : defaults.Attempts;
        var delayMilliseconds = int.TryParse(
            configuration["Infrastructure:DuplicateRead:DelayMilliseconds"],
            out var parsedDelayMilliseconds)
            ? parsedDelayMilliseconds
            : defaults.DelayMilliseconds;
        var ledgerServiceOptions = new LedgerServiceOptions
        {
            Attempts = attempts,
            DelayMilliseconds = delayMilliseconds
        };

        services.AddSingleton(Options.Create(ledgerServiceOptions));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ILedgerService, LedgerService>();

        return services;
    }
}
