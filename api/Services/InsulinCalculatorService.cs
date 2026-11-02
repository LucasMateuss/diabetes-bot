using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;

namespace DiabetesBot.Services;

public class InsulinCalculatorService : IInsulinCalculatorService
{
    private readonly ILogger<InsulinCalculatorService> _logger;

    public InsulinCalculatorService(ILogger<InsulinCalculatorService> logger)
    {
        _logger = logger;
    }

    public decimal CalculateMealBolus(decimal totalCarbs, decimal ric)
    {
        if (ric <= 0)
        {
            _logger.LogWarning("RIC inválido informado: {RIC}.", ric);
            return 0;
        }

        return totalCarbs / ric;
    }

    public decimal CalculateCorrectionBolus(int currentGlycemia, int targetGlycemia, decimal sensitivityFactor)
    {
        if (sensitivityFactor <= 0)
        {
            _logger.LogWarning("Fator de sensibilidade inválido informado: {FS}.", sensitivityFactor);
            return 0;
        }

        return (currentGlycemia - targetGlycemia) / sensitivityFactor;
    }

    public InsulinResult CalculateTotalDose(
        List<NutritionResult> alimentos,
        decimal ric,
        decimal fs,
        int glicemiaAlvo,
        int? glicemiaAtual = null)
    {
        var totalCarbs = alimentos
            .Where(a => a.Encontrado)
            .Sum(a => a.TotalCarboidratos);

        var bolusRefeicao = CalculateMealBolus(totalCarbs, ric);

        decimal? bolusCorrecao = glicemiaAtual.HasValue
            ? CalculateCorrectionBolus(glicemiaAtual.Value, glicemiaAlvo, fs)
            : null;

        var doseTotal = Math.Max(0, bolusRefeicao + (bolusCorrecao ?? 0));
        doseTotal = RoundToHalfUnit(doseTotal);

        return new InsulinResult
        {
            Alimentos = alimentos,
            TotalCarboidratos = Math.Round(totalCarbs, 1),
            BolusRefeicao = Math.Round(bolusRefeicao, 2),
            BolusCorrecao = bolusCorrecao.HasValue ? Math.Round(bolusCorrecao.Value, 2) : null,
            DoseTotal = doseTotal,
            GlicemiaInformada = glicemiaAtual,
            RICUtilizada = ric,
            FSUtilizado = fs,
            GlicemiaAlvo = glicemiaAlvo
        };
    }

    private static decimal RoundToHalfUnit(decimal value)
    {
        return Math.Round(value * 2, MidpointRounding.ToEven) / 2;
    }
}
