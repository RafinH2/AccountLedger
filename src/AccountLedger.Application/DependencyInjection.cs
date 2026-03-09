using Microsoft.Extensions.DependencyInjection;

namespace AccountLedger.Application;

/// <summary>
/// Методы регистрации зависимостей прикладного слоя.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует обработчики MediatR из текущей сборки.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Обновленная коллекция сервисов.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
