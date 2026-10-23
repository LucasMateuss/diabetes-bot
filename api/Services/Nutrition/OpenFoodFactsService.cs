using System.Globalization;
using System.Text.Json;
using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;

namespace DiabetesBot.Services.Nutrition;

public class OpenFoodFactsService : IOpenFoodFactsService
{
    private const string BaseUrl = "https://br.openfoodfacts.org";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsService> _logger;

    public OpenFoodFactsService(HttpClient httpClient, ILogger<OpenFoodFactsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DiabetesBot/1.0 (TCC - Sistemas de Informacao)");
        }
    }

    public async Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams, decimal? packageUnits = null)
    {
        try
        {
            var url = $"{BaseUrl}/cgi/search.pl?search_terms={Uri.EscapeDataString(foodName)}" +
                      "&search_simple=1&action=process&json=1&page_size=10&fields=product_name,brands,nutriments,serving_quantity";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("products", out var products) ||
                products.GetArrayLength() == 0)
            {
                return null;
            }

            var searchKeywords = foodName.ToLowerInvariant()
                .Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            var rankedProducts = products.EnumerateArray()
                .Select(p =>
                {
                    var name = p.TryGetProperty("product_name", out var n) ? n.GetString() ?? "" : "";
                    var brand = p.TryGetProperty("brands", out var b) ? b.GetString() ?? "" : "";
                    var matchCount = searchKeywords.Count(k =>
                        name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                        brand.Contains(k, StringComparison.OrdinalIgnoreCase));
                    return (Product: p, Name: name, Brand: brand, MatchCount: matchCount);
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) && (searchKeywords.Count == 0 || x.MatchCount > 0))
                .OrderByDescending(x => x.MatchCount)
                .ThenBy(x => x.Name.Length);

            foreach (var item in rankedProducts)
            {
                var product = item.Product;

                if (!product.TryGetProperty("nutriments", out var nutriments))
                    continue;

                decimal carbsPer100g = 0;
                if (nutriments.TryGetProperty("carbohydrates_100g", out var carbs) &&
                    carbs.ValueKind == JsonValueKind.Number)
                {
                    carbsPer100g = carbs.GetDecimal();
                }
                else if (nutriments.TryGetProperty("carbohydrates", out var carbsAlt) &&
                         carbsAlt.ValueKind == JsonValueKind.Number)
                {
                    carbsPer100g = carbsAlt.GetDecimal();
                }

                if (carbsPer100g <= 0)
                    continue;

                var finalGrams = quantityGrams;
                if (packageUnits is > 0 && TryExtractServingQuantity(product, out var servingQuantity))
                {
                    finalGrams = Math.Round(servingQuantity * packageUnits.Value, 0);
                }

                var cleanName = item.Name.Replace(",", " ").Trim();
                var primaryBrand = item.Brand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
                var fullDisplayName = !string.IsNullOrWhiteSpace(primaryBrand) &&
                                      !cleanName.Contains(primaryBrand, StringComparison.OrdinalIgnoreCase)
                    ? $"{cleanName} - {primaryBrand}"
                    : cleanName;

                return new NutritionResult
                {
                    NomeEncontrado = fullDisplayName,
                    CarboidratoPor100g = carbsPer100g,
                    QuantidadeGramas = finalGrams,
                    Fonte = FonteNutricional.OpenFoodFacts,
                    Encontrado = true
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar Open Food Facts para '{FoodName}'.", foodName);
            return null;
        }
    }

    private static bool TryExtractServingQuantity(JsonElement product, out decimal servingQuantity)
    {
        servingQuantity = 0;

        if (!product.TryGetProperty("serving_quantity", out var servingProp))
            return false;

        if (servingProp.ValueKind == JsonValueKind.Number)
        {
            servingQuantity = servingProp.GetDecimal();
        }
        else if (servingProp.ValueKind == JsonValueKind.String)
        {
            var raw = servingProp.GetString()?.Trim().Replace(",", ".");
            if (!string.IsNullOrEmpty(raw))
            {
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out servingQuantity);
            }
        }

        return servingQuantity is > 0 and <= 500;
    }
}
