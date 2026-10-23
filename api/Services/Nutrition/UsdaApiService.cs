using System.Text.Json;
using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;

namespace DiabetesBot.Services.Nutrition;

public class UsdaApiService : IUsdaApiService
{
    private const string BaseUrl = "https://api.nal.usda.gov/fdc/v1";
    private const int CarbohydrateNutrientId = 1005;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<UsdaApiService> _logger;

    public UsdaApiService(HttpClient httpClient, ILogger<UsdaApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("USDA_API_KEY") ?? string.Empty;
    }

    public async Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        try
        {
            var url = $"{BaseUrl}/foods/search?api_key={_apiKey}&query={Uri.EscapeDataString(foodName)}&pageSize=5&dataType=Foundation,SR%20Legacy";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var foods = doc.RootElement.GetProperty("foods");
            if (foods.GetArrayLength() == 0)
                return null;

            var firstFood = foods[0];
            var description = firstFood.GetProperty("description").GetString() ?? foodName;

            decimal carbsPer100g = 0;
            if (firstFood.TryGetProperty("foodNutrients", out var nutrients))
            {
                foreach (var nutrient in nutrients.EnumerateArray())
                {
                    if (nutrient.TryGetProperty("nutrientId", out var nutrientId) &&
                        nutrientId.GetInt32() == CarbohydrateNutrientId)
                    {
                        carbsPer100g = nutrient.TryGetProperty("value", out var value)
                            ? value.GetDecimal()
                            : 0;
                        break;
                    }
                }
            }

            return new NutritionResult
            {
                NomeEncontrado = description,
                CarboidratoPor100g = carbsPer100g,
                QuantidadeGramas = quantityGrams,
                Fonte = FonteNutricional.USDA,
                Encontrado = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar USDA API para '{FoodName}'.", foodName);
            return null;
        }
    }
}
