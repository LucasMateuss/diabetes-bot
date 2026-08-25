using DiabetesBot.Enums;

namespace DiabetesBot.Entities;

public class RegistroRefeicao
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PacienteId { get; private set; }
    public string DescricaoOriginal { get; private set; } = string.Empty;
    public string AlimentosJson { get; private set; } = string.Empty;
    public decimal TotalCarboidratos { get; private set; }
    public decimal DoseCalculada { get; private set; }
    public int? GlicemiaInformada { get; private set; }
    public FonteNutricional FonteNutricional { get; private set; }
    public DateTime DataRegistro { get; private set; } = DateTime.UtcNow;
    public Paciente Paciente { get; private set; } = null!;

    protected RegistroRefeicao() { }

    public RegistroRefeicao(
        Guid pacienteId,
        string descricaoOriginal,
        string alimentosJson,
        decimal totalCarboidratos,
        decimal doseCalculada,
        int? glicemiaInformada,
        FonteNutricional fonteNutricional)
    {
        PacienteId = pacienteId;
        DescricaoOriginal = descricaoOriginal;
        AlimentosJson = alimentosJson;
        TotalCarboidratos = totalCarboidratos;
        DoseCalculada = doseCalculada;
        GlicemiaInformada = glicemiaInformada;
        FonteNutricional = fonteNutricional;
    }
}
