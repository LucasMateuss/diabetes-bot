using DiabetesBot.Models.Nutrition;

namespace DiabetesBot.Services.Interfaces;

public interface IWhatsAppService
{
    Task SendTextMessageAsync(string toPhoneNumber, string message);
}

public interface IGeminiService
{
    Task<List<FoodItem>> ParseFoodDescriptionAsync(string userMessage);
}

public interface IInsulinCalculatorService
{
    decimal CalculateMealBolus(decimal totalCarbs, decimal ric);
    decimal CalculateCorrectionBolus(int currentGlycemia, int targetGlycemia, decimal sensitivityFactor);
    InsulinResult CalculateTotalDose(
        List<NutritionResult> alimentos,
        decimal ric,
        decimal fs,
        int glicemiaAlvo,
        int? glicemiaAtual = null);
}

public interface IBotOrchestratorService
{
    Task ProcessMessageAsync(string phoneNumber, string message);
}

public interface INutritionService
{
    Task<NutritionResult> SearchFoodAsync(
        string foodName,
        decimal quantityGrams,
        string? englishName = null,
        bool preferOpenFoodFacts = false,
        decimal? packageUnits = null);
    Task<List<NutritionResult>> SearchFoodsAsync(List<FoodItem> foodItems);
}

public interface ITacoLookupService
{
    Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams);
}

public interface IUsdaApiService
{
    Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams);
}

public interface IOpenFoodFactsService
{
    Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams, decimal? packageUnits = null);
}
