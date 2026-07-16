namespace DiabetesBot.Entities;

public class Paciente
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Nome { get; private set; } = string.Empty;

    public string TelefoneWhatsApp { get; private set; } = string.Empty;

    public decimal FatorSensibilidade { get; private set; }

    public decimal RelacaoInsulinaCarboidrato { get; private set; }

    public int GlicemiaAlvo { get; private set; }

    public DateTime DataCadastro { get; private set; } = DateTime.UtcNow;

    protected Paciente() { }

    public Paciente(string nome, string telefoneWhatsApp, decimal fatorSensibilidade, decimal relacaoInsulinaCarboidrato, int glicemiaAlvo)
    {
        Nome = nome;
        TelefoneWhatsApp = telefoneWhatsApp;
        FatorSensibilidade = fatorSensibilidade;
        RelacaoInsulinaCarboidrato = relacaoInsulinaCarboidrato;
        GlicemiaAlvo = glicemiaAlvo;
    }
}