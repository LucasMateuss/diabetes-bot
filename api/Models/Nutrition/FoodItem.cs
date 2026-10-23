using System.Text.Json.Serialization;

namespace DiabetesBot.Models.Nutrition;

public class FoodItem
{
    [JsonPropertyName("alimento")]
    public string Alimento { get; set; } = string.Empty;

    [JsonPropertyName("nome_exibicao")]
    public string? NomeExibicao { get; set; }

    [JsonPropertyName("alimento_en")]
    public string? AlimentoIngles { get; set; }

    [JsonPropertyName("medida_informada")]
    public string? MedidaInformada { get; set; }

    [JsonPropertyName("quantidade_gramas")]
    public decimal QuantidadeGramas { get; set; }

    [JsonPropertyName("industrializado")]
    public bool Industrializado { get; set; }

    [JsonPropertyName("unidades_embalagem")]
    public decimal? UnidadesEmbalagem { get; set; }
}
