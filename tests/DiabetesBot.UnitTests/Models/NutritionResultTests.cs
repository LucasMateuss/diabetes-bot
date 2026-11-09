using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using FluentAssertions;
using Xunit;

namespace DiabetesBot.UnitTests.Models;

public class NutritionResultTests
{
    [Theory]
    [InlineData(28.0, 100.0, 28.0)]
    [InlineData(28.0, 150.0, 42.0)]
    [InlineData(14.0, 50.0, 7.0)]
    [InlineData(58.7, 50.0, 29.35)]
    [InlineData(0.0, 200.0, 0.0)]
    [InlineData(30.0, 0.0, 0.0)]
    public void TotalCarboidratos_CalculatesProportionalCarbs(
        decimal carbPer100g, decimal quantityGrams, decimal expectedTotal)
    {
        var result = new NutritionResult
        {
            NomeEncontrado = "Alimento Teste",
            CarboidratoPor100g = carbPer100g,
            QuantidadeGramas = quantityGrams,
            Fonte = FonteNutricional.TACO,
            Encontrado = true
        };

        result.TotalCarboidratos.Should().Be(expectedTotal);
    }
}
