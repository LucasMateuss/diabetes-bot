namespace DiabetesBot.Entities;

public class Alimento
{
    public int Id { get; private set; }
    public int CodigoTaco { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Grupo { get; private set; } = string.Empty;
    public decimal CarboidratoPor100g { get; private set; }
    public decimal ProteinaPor100g { get; private set; }
    public decimal LipidioPor100g { get; private set; }
    public decimal FibraPor100g { get; private set; }
    public decimal EnergiaPor100g { get; private set; }

    protected Alimento() { }

    public Alimento(
        int codigoTaco,
        string nome,
        string grupo,
        decimal carboidratoPor100g,
        decimal proteinaPor100g,
        decimal lipidioPor100g,
        decimal fibraPor100g,
        decimal energiaPor100g)
    {
        CodigoTaco = codigoTaco;
        Nome = nome;
        Grupo = grupo;
        CarboidratoPor100g = carboidratoPor100g;
        ProteinaPor100g = proteinaPor100g;
        LipidioPor100g = lipidioPor100g;
        FibraPor100g = fibraPor100g;
        EnergiaPor100g = energiaPor100g;
    }
}
