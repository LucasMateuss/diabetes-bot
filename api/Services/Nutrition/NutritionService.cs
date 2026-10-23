using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;

namespace DiabetesBot.Services.Nutrition;

public class NutritionService : INutritionService
{
    private readonly ITacoLookupService _tacoService;
    private readonly IUsdaApiService _usdaService;
    private readonly IOpenFoodFactsService _openFoodFactsService;
    private readonly ILogger<NutritionService> _logger;

    public NutritionService(
        ITacoLookupService tacoService,
        IUsdaApiService usdaService,
        IOpenFoodFactsService openFoodFactsService,
        ILogger<NutritionService> logger)
    {
        _tacoService = tacoService;
        _usdaService = usdaService;
        _openFoodFactsService = openFoodFactsService;
        _logger = logger;
    }

    public async Task<NutritionResult> SearchFoodAsync(
        string foodName,
        decimal quantityGrams,
        string? englishName = null,
        bool preferOpenFoodFacts = false,
        decimal? packageUnits = null)
    {
        var result = await _tacoService.SearchAsync(foodName, quantityGrams);
        if (result != null)
            return result;

        if (preferOpenFoodFacts)
        {
            result = await _openFoodFactsService.SearchAsync(foodName, quantityGrams, packageUnits);
            if (result != null)
                return result;
        }

        var usdaQuery = !string.IsNullOrWhiteSpace(englishName) ? englishName : foodName;
        result = await _usdaService.SearchAsync(usdaQuery, quantityGrams);
        if (result != null)
        {
            result.NomeEncontrado = TacoLookupService.CleanTacoDisplayName(foodName);
            return result;
        }

        if (!preferOpenFoodFacts)
        {
            result = await _openFoodFactsService.SearchAsync(foodName, quantityGrams, packageUnits);
            if (result != null)
                return result;
        }

        _logger.LogWarning("Alimento não encontrado em nenhuma fonte nutricional: {FoodName}", foodName);

        return new NutritionResult
        {
            NomeEncontrado = TacoLookupService.CleanTacoDisplayName(foodName),
            CarboidratoPor100g = 0,
            QuantidadeGramas = quantityGrams,
            Fonte = FonteNutricional.TACO,
            Encontrado = false
        };
    }

    public async Task<List<NutritionResult>> SearchFoodsAsync(List<FoodItem> foodItems)
    {
        var results = new List<NutritionResult>(foodItems.Count);

        foreach (var item in foodItems)
        {
            var result = await SearchFoodAsync(
                item.Alimento,
                item.QuantidadeGramas,
                item.AlimentoIngles,
                item.Industrializado,
                item.UnidadesEmbalagem);

            if (!string.IsNullOrWhiteSpace(item.NomeExibicao) &&
                (!result.Encontrado || result.Fonte == FonteNutricional.USDA))
            {
                result.NomeEncontrado = item.NomeExibicao;
            }

            result.MedidaInformada = item.MedidaInformada;
            results.Add(result);
        }

        return results;
    }
}
