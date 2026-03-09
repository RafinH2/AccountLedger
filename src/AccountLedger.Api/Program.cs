using AccountLedger.Api.Configuration;
using AccountLedger.Api.ErrorHandling;
using AccountLedger.Application;
using AccountLedger.Infrastructure;
using AccountLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
ConfigureYamlConfiguration(builder.Configuration, builder.Environment, args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<DatabaseMigrationOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseMigrationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<SwaggerOptions>()
    .Bind(builder.Configuration.GetSection(SwaggerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9457",
            Title = "Validation error",
            Status = 400,
            Detail = "Request validation failed.",
            Instance = context.HttpContext.Request.Path
        };

        details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(details);
    };
});

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

var swaggerOptions = app.Services.GetRequiredService<IOptions<SwaggerOptions>>().Value;
if (swaggerOptions.Enabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    var migrationOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseMigrationOptions>>().Value;
    await MigrateWithRetryAsync(dbContext, migrationOptions, app.Lifetime.ApplicationStopping);
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.Run();

return;

static void ConfigureYamlConfiguration(
    ConfigurationManager configuration,
    IWebHostEnvironment environment,
    IReadOnlyList<string> args)
{
    configuration.Sources.Clear();
    configuration
        .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true)
        .AddYamlFile($"appsettings.{environment.EnvironmentName}.yml", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    if (args.Count > 0)
    {
        configuration.AddCommandLine(args.ToArray());
    }
}

static async Task MigrateWithRetryAsync(
    LedgerDbContext dbContext,
    DatabaseMigrationOptions options,
    CancellationToken cancellationToken)
{
    var maxAttempts = Math.Max(1, options.MaxAttempts);
    var delay = TimeSpan.FromSeconds(Math.Max(0, options.DelaySeconds));

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }
        catch when (attempt < maxAttempts)
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    await dbContext.Database.MigrateAsync(cancellationToken);
}

/// <summary>
/// Маркерный тип точки входа для сценариев интеграционного тестирования.
/// </summary>
public partial class Program;
