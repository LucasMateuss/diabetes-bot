using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;
using DiabetesBot.Services.Nutrition;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiabetesBot.UnitTests.Services;

public class NutritionServiceTests
{
    private readonly Mock<ITacoLookupService> _tacoMock;
    private readonly Mock<IUsdaApiService> _usdaMock;
    private readonly Mock<IOpenFoodFactsService> _offMock;
    private readonly Mock<ILogger<NutritionService>> _loggerMock;
    private readonly NutritionService _sut;

    public NutritionServiceTests()
    {
        _tacoMock = new Mock<ITacoLookupService>();
        _usdaMock = new Mock<IUsdaApiService>();
        _offMock = new Mock<IOpenFoodFactsService>();
        _loggerMock = new Mock<ILogger<NutritionService>>();

        _sut = new NutritionService(
            _tacoMock.Object,
            _usdaMock.Object,
            _offMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SearchFoodAsync_FoundInTaco_ReturnsTacoResultAndDoesNotCallOtherServices()
    {
        var tacoResult = new NutritionResult
        {
            NomeEncontrado = "Arroz, tipo 1, cozido",
            CarboidratoPor100g = 28.1m,
            QuantidadeGramas = 100m,
            Fonte = FonteNutricional.TACO,
            Encontrado = true
        };

        _tacoMock
            .Setup(t => t.SearchAsync("arroz", 100m))
            .ReturnsAsync(tacoResult);

        var result = await _sut.SearchFoodAsync("arroz", 100m);

        result.Should().BeEquivalentTo(tacoResult);
        result.Fonte.Should().Be(FonteNutricional.TACO);
        result.Encontrado.Should().BeTrue();

        _usdaMock.Verify(u => u.SearchAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        _offMock.Verify(o => o.SearchAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal?>()), Times.Never);
    }

    [Fact]
    public async Task SearchFoodAsync_NotFoundInTaco_FallsBackToUsda()
    {
        _tacoMock
            .Setup(t => t.SearchAsync("quinoa", 150m))
            .ReturnsAsync((NutritionResult?)null);

        var usdaResult = new NutritionResult
        {
            NomeEncontrado = "quinoa",
            CarboidratoPor100g = 21.3m,
            QuantidadeGramas = 150m,
            Fonte = FonteNutricional.USDA,
            Encontrado = true
        };

        _usdaMock
            .Setup(u => u.SearchAsync("quinoa", 150m))
            .ReturnsAsync(usdaResult);

        var result = await _sut.SearchFoodAsync("quinoa", 150m);

        result.Should().BeEquivalentTo(usdaResult);
        result.Fonte.Should().Be(FonteNutricional.USDA);
        result.Encontrado.Should().BeTrue();

        _offMock.Verify(o => o.SearchAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<decimal?>()), Times.Never);
    }

    [Fact]
    public async Task SearchFoodAsync_NotFoundInTacoOrUsda_FallsBackToOpenFoodFacts()
    {
        _tacoMock
            .Setup(t => t.SearchAsync("biscoito oreo", 60m))
            .ReturnsAsync((NutritionResult?)null);

        _usdaMock
            .Setup(u => u.SearchAsync("biscoito oreo", 60m))
            .ReturnsAsync((NutritionResult?)null);

        var offResult = new NutritionResult
        {
            NomeEncontrado = "Oreo Original",
            CarboidratoPor100g = 68.0m,
            QuantidadeGramas = 60m,
            Fonte = FonteNutricional.OpenFoodFacts,
            Encontrado = true
        };

        _offMock
            .Setup(o => o.SearchAsync("biscoito oreo", 60m, null))
            .ReturnsAsync(offResult);

        var result = await _sut.SearchFoodAsync("biscoito oreo", 60m);

        result.Should().BeEquivalentTo(offResult);
        result.Fonte.Should().Be(FonteNutricional.OpenFoodFacts);
        result.Encontrado.Should().BeTrue();
    }

    [Fact]
    public async Task SearchFoodAsync_IndustrializedProduct_PrioritizesOpenFoodFactsOverUsda()
    {
        _tacoMock
            .Setup(t => t.SearchAsync("Magic Toast", 18m))
            .ReturnsAsync((NutritionResult?)null);

        var offResult = new NutritionResult
        {
            NomeEncontrado = "Magic Toast Original",
            CarboidratoPor100g = 77.8m,
            QuantidadeGramas = 18m,
            Fonte = FonteNutricional.OpenFoodFacts,
            Encontrado = true
        };

        _offMock
            .Setup(o => o.SearchAsync("Magic Toast", 18m, 1m))
            .ReturnsAsync(offResult);

        var result = await _sut.SearchFoodAsync("Magic Toast", 18m, "Crispbread toast", preferOpenFoodFacts: true, packageUnits: 1m);

        result.Should().BeEquivalentTo(offResult);
        result.Fonte.Should().Be(FonteNutricional.OpenFoodFacts);
        _usdaMock.Verify(u => u.SearchAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task SearchFoodAsync_NotFoundInAnySource_ReturnsEncontradoFalse()
    {
        _tacoMock
            .Setup(t => t.SearchAsync("item inexistente", 100m))
            .ReturnsAsync((NutritionResult?)null);

        _usdaMock
            .Setup(u => u.SearchAsync("item inexistente", 100m))
            .ReturnsAsync((NutritionResult?)null);

        _offMock
            .Setup(o => o.SearchAsync("item inexistente", 100m, null))
            .ReturnsAsync((NutritionResult?)null);

        var result = await _sut.SearchFoodAsync("item inexistente", 100m);

        result.Encontrado.Should().BeFalse();
        result.NomeEncontrado.Should().Be("item inexistente");
        result.CarboidratoPor100g.Should().Be(0m);
        result.QuantidadeGramas.Should().Be(100m);
    }

    [Fact]
    public async Task SearchFoodsAsync_MultipleItems_ReturnsAllResults()
    {
        var foodItems = new List<FoodItem>
        {
            new() { Alimento = "Arroz", QuantidadeGramas = 100m },
            new() { Alimento = "Feijão", QuantidadeGramas = 80m }
        };

        _tacoMock
            .Setup(t => t.SearchAsync("Arroz", 100m))
            .ReturnsAsync(new NutritionResult
            {
                NomeEncontrado = "Arroz",
                CarboidratoPor100g = 28m,
                QuantidadeGramas = 100m,
                Fonte = FonteNutricional.TACO,
                Encontrado = true
            });

        _tacoMock
            .Setup(t => t.SearchAsync("Feijão", 80m))
            .ReturnsAsync(new NutritionResult
            {
                NomeEncontrado = "Feijão",
                CarboidratoPor100g = 14m,
                QuantidadeGramas = 80m,
                Fonte = FonteNutricional.TACO,
                Encontrado = true
            });

        var results = await _sut.SearchFoodsAsync(foodItems);

        results.Should().HaveCount(2);
        results[0].NomeEncontrado.Should().Be("Arroz");
        results[1].NomeEncontrado.Should().Be("Feijão");
    }
}
