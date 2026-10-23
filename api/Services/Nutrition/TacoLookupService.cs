using DiabetesBot.Data;
using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiabetesBot.Services.Nutrition;

public class TacoLookupService : ITacoLookupService
{
    private static readonly char[] Separators = [' ', ',', '.', ';', '-', '/', '(', ')'];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "com", "sem", "para", "tipo", "uma", "uns", "sabor", "por"
    };

    private static readonly HashSet<string> OptionalDescriptors = new(StringComparer.OrdinalIgnoreCase)
    {
        "cozido", "cozida", "cru", "crua", "frito", "frita",
        "grelhado", "grelhada", "assado", "assada", "inteiro",
        "inteira", "cremoso", "cremosa", "tradicional", "refogado", "refogada",
        "vaca", "galinha", "trigo", "integral", "desnatado", "infusão"
    };

    private static readonly HashSet<string> SpecificVariantWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "coco", "cabra", "condensado", "fermentado", "achocolatado",
        "pó", "codorna", "doce", "baroa", "puba", "bucho", "fígado",
        "coração", "moela", "língua", "cupim", "paulista", "mistura",
        "wafer", "recheado", "polvilho", "dietético", "diet", "congelado", "congelada"
    };

    private static readonly HashSet<string> RawTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "cru", "crua", "crus", "cruas"
    };

    private readonly AppDbContext _dbContext;

    public TacoLookupService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NutritionResult?> SearchAsync(string foodName, decimal quantityGrams)
    {
        var normalizedName = foodName.Trim().ToLowerInvariant();

        var alimento = await _dbContext.Alimentos
            .FirstOrDefaultAsync(a => a.Nome.ToLower() == normalizedName);

        if (alimento == null)
        {
            var keywords = normalizedName
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !StopWords.Contains(w))
                .ToList();

            if (keywords.Count > 0)
            {
                var primaryKeyword = keywords[0];
                var requestedRaw = keywords.Any(k => RawTokens.Contains(k));

                var requiredIdentityKeywords = keywords
                    .Where(k => k.Length > 2 && !OptionalDescriptors.Contains(k))
                    .ToList();

                var candidates = await _dbContext.Alimentos
                    .Where(a => a.Nome.ToLower().StartsWith(primaryKeyword))
                    .ToListAsync();

                alimento = candidates
                    .Where(a =>
                    {
                        var candidateTokens = a.Nome.ToLowerInvariant()
                            .Split(Separators, StringSplitOptions.RemoveEmptyEntries);

                        var hasAllRequired = requiredIdentityKeywords.All(req =>
                            candidateTokens.Any(t => t.StartsWith(req) || req.StartsWith(t)));

                        if (!hasAllRequired)
                            return false;

                        var hasUnrequestedSpecificVariant = candidateTokens.Any(t =>
                            SpecificVariantWords.Contains(t) && !keywords.Contains(t));

                        return !hasUnrequestedSpecificVariant;
                    })
                    .OrderByDescending(a => keywords.Count(k => a.Nome.ToLowerInvariant().Contains(k)))
                    .ThenBy(a => !requestedRaw && HasRawToken(a.Nome) ? 1 : 0)
                    .ThenBy(a => a.Nome.Length)
                    .FirstOrDefault();
            }
        }

        if (alimento == null)
            return null;

        return new NutritionResult
        {
            NomeEncontrado = CleanTacoDisplayName(alimento.Nome),
            CarboidratoPor100g = alimento.CarboidratoPor100g,
            QuantidadeGramas = quantityGrams,
            Fonte = FonteNutricional.TACO,
            Encontrado = true
        };
    }

    private static bool HasRawToken(string foodName)
    {
        var tokens = foodName.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(t => RawTokens.Contains(t));
    }

    public static string CleanTacoDisplayName(string tacoName)
    {
        if (!tacoName.Contains(','))
            return tacoName;

        var parts = tacoName
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Replace("/10minutos", " (10 minutos)", StringComparison.OrdinalIgnoreCase).Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        return string.Join(" ", parts);
    }
}
