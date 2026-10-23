using DiabetesBot.Enums;

namespace DiabetesBot.Models.Nutrition;

public class NutritionResult
{
    public string NomeEncontrado { get; set; } = string.Empty;
    public string? MedidaInformada { get; set; }
    public decimal CarboidratoPor100g { get; set; }
    public decimal QuantidadeGramas { get; set; }
    public decimal TotalCarboidratos => (CarboidratoPor100g * QuantidadeGramas) / 100m;
    public FonteNutricional Fonte { get; set; }
    public bool Encontrado { get; set; }
}
