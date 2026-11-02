namespace DiabetesBot.Models.Nutrition;

public class InsulinResult
{
    public List<NutritionResult> Alimentos { get; set; } = new();
    public decimal TotalCarboidratos { get; set; }
    public decimal BolusRefeicao { get; set; }
    public decimal? BolusCorrecao { get; set; }
    public decimal DoseTotal { get; set; }
    public int? GlicemiaInformada { get; set; }
    public decimal RICUtilizada { get; set; }
    public decimal FSUtilizado { get; set; }
    public int GlicemiaAlvo { get; set; }
}
