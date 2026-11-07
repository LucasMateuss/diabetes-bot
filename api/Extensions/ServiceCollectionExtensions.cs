using System.Net.Http.Headers;
using DiabetesBot.Data;
using DiabetesBot.Services;
using DiabetesBot.Services.Interfaces;
using DiabetesBot.Services.Nutrition;
using Microsoft.EntityFrameworkCore;

namespace DiabetesBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");

        var connectionString = $"Host=localhost;Port=5432;Database={dbName};Username={dbUser};Password={dbPass}";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services)
    {
        services.AddHttpClient<IWhatsAppService, WhatsAppService>(client =>
        {
            var baseUrl = Environment.GetEnvironmentVariable("EVOLUTION_API_URL") ?? "http://localhost:8080";
            var instanceToken = Environment.GetEnvironmentVariable("EVOLUTION_INSTANCE_TOKEN") ?? string.Empty;

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("apikey", instanceToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient<IUsdaApiService, UsdaApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(25);
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITacoLookupService, TacoLookupService>();
        services.AddScoped<INutritionService, NutritionService>();
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<IInsulinCalculatorService, InsulinCalculatorService>();
        services.AddScoped<IBotOrchestratorService, BotOrchestratorService>();

        return services;
    }
}
