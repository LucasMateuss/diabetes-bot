using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiabetesBot.UnitTests.Services;

public class InsulinCalculatorServiceTests
{
    private readonly InsulinCalculatorService _sut;
    private readonly Mock<ILogger<InsulinCalculatorService>> _loggerMock;

    public InsulinCalculatorServiceTests()
    {
        _loggerMock = new Mock<ILogger<InsulinCalculatorService>>();
        _sut = new InsulinCalculatorService(_loggerMock.Object);
    }

    [Fact]
    public void CalculateMealBolus_ValidCarbsAndRic_ReturnsCorrectBolus()
    {
        var result = _sut.CalculateMealBolus(totalCarbs: 60m, ric: 15m);

        result.Should().Be(4.0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CalculateMealBolus_ZeroOrNegativeRic_ReturnsZero(decimal invalidRic)
    {
        var result = _sut.CalculateMealBolus(50m, invalidRic);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateCorrectionBolus_GlycemiaAboveTarget_ReturnsPositiveCorrection()
    {
        var result = _sut.CalculateCorrectionBolus(currentGlycemia: 220, targetGlycemia: 120, sensitivityFactor: 50m);

        result.Should().Be(2.0m);
    }

    [Fact]
    public void CalculateCorrectionBolus_GlycemiaBelowTarget_ReturnsNegativeCorrection()
    {
        var result = _sut.CalculateCorrectionBolus(currentGlycemia: 70, targetGlycemia: 120, sensitivityFactor: 50m);

        result.Should().Be(-1.0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CalculateCorrectionBolus_ZeroOrNegativeFS_ReturnsZero(decimal invalidFs)
    {
        var result = _sut.CalculateCorrectionBolus(200, 120, invalidFs);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotalDose_WithoutCurrentGlycemia_CalculatesOnlyMealBolus()
    {
        var foods = new List<NutritionResult>
        {
            new()
            {
                NomeEncontrado = "Arroz",
                CarboidratoPor100g = 28m,
                QuantidadeGramas = 100m,
                Encontrado = true,
                Fonte = FonteNutricional.TACO
            },
            new()
            {
                NomeEncontrado = "Feijão",
                CarboidratoPor100g = 14m,
                QuantidadeGramas = 100m,
                Encontrado = true,
                Fonte = FonteNutricional.TACO
            }
        };

        var result = _sut.CalculateTotalDose(foods, ric: 15m, fs: 50m, glicemiaAlvo: 120, glicemiaAtual: null);

        result.TotalCarboidratos.Should().Be(42.0m);
        result.BolusRefeicao.Should().Be(2.8m);
        result.BolusCorrecao.Should().BeNull();
        result.GlicemiaInformada.Should().BeNull();
        result.DoseTotal.Should().Be(3.0m);
    }

    [Fact]
    public void CalculateTotalDose_WithHighGlycemia_AddsCorrectionBolus()
    {
        var foods = new List<NutritionResult>
        {
            new()
            {
                NomeEncontrado = "Pão Francês",
                CarboidratoPor100g = 58.7m,
                QuantidadeGramas = 50m,
                Encontrado = true,
                Fonte = FonteNutricional.TACO
            }
        };

        var result = _sut.CalculateTotalDose(foods, ric: 15m, fs: 50m, glicemiaAlvo: 120, glicemiaAtual: 220);

        result.BolusCorrecao.Should().Be(2.0m);
        result.DoseTotal.Should().Be(4.0m);
    }

    [Fact]
    public void CalculateTotalDose_WhenCorrectionCausesNegativeSum_ClampsDoseToZero()
    {
        var foods = new List<NutritionResult>
        {
            new()
            {
                NomeEncontrado = "Gelatina diet",
                CarboidratoPor100g = 5m,
                QuantidadeGramas = 100m,
                Encontrado = true,
                Fonte = FonteNutricional.TACO
            }
        };

        var result = _sut.CalculateTotalDose(foods, ric: 15m, fs: 30m, glicemiaAlvo: 120, glicemiaAtual: 50);

        result.DoseTotal.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotalDose_IgnoresFoodsNotMarkedAsFound()
    {
        var foods = new List<NutritionResult>
        {
            new()
            {
                NomeEncontrado = "Arroz",
                CarboidratoPor100g = 28m,
                QuantidadeGramas = 100m,
                Encontrado = true
            },
            new()
            {
                NomeEncontrado = "Alimento Inexistente",
                CarboidratoPor100g = 80m,
                QuantidadeGramas = 200m,
                Encontrado = false
            }
        };

        var result = _sut.CalculateTotalDose(foods, ric: 14m, fs: 50m, glicemiaAlvo: 120);

        result.TotalCarboidratos.Should().Be(28m);
        result.DoseTotal.Should().Be(2.0m);
    }

    [Theory]
    [InlineData(3.0, 3.0)]
    [InlineData(3.1, 3.0)]
    [InlineData(3.2, 3.0)]
    [InlineData(3.3, 3.5)]
    [InlineData(3.4, 3.5)]
    [InlineData(3.5, 3.5)]
    [InlineData(3.6, 3.5)]
    [InlineData(3.7, 3.5)]
    [InlineData(3.8, 4.0)]
    [InlineData(3.9, 4.0)]
    [InlineData(4.0, 4.0)]
    public void CalculateTotalDose_RoundsToNearestHalfUnit_Properly(decimal rawDose, decimal expectedRoundedDose)
    {
        var foods = new List<NutritionResult>
        {
            new()
            {
                NomeEncontrado = "Alimento Teste",
                CarboidratoPor100g = rawDose * 10m,
                QuantidadeGramas = 10m,
                Encontrado = true
            }
        };

        var result = _sut.CalculateTotalDose(foods, ric: 1m, fs: 50m, glicemiaAlvo: 120, glicemiaAtual: null);

        result.DoseTotal.Should().Be(expectedRoundedDose);
    }
}
