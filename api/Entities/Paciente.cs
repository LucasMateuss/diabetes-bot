using DiabetesBot.Enums;

namespace DiabetesBot.Entities;

public class Paciente
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Nome { get; private set; } = string.Empty;
    public string TelefoneWhatsApp { get; private set; } = string.Empty;
    public decimal FatorSensibilidade { get; private set; }
    public decimal RelacaoInsulinaCarboidrato { get; private set; }
    public int GlicemiaAlvo { get; private set; }
    public bool Ativo { get; private set; } = true;
    public EstadoCadastro EstadoCadastro { get; private set; } = EstadoCadastro.NaoRegistrado;
    public DateTime DataCadastro { get; private set; } = DateTime.UtcNow;

    protected Paciente() { }

    public Paciente(string telefoneWhatsApp)
    {
        TelefoneWhatsApp = telefoneWhatsApp;
        EstadoCadastro = EstadoCadastro.AguardandoNome;
    }

    public void DefinirNome(string nome)
    {
        Nome = nome;
        EstadoCadastro = EstadoCadastro.AguardandoRIC;
    }

    public void DefinirRelacaoInsulinaCarboidrato(decimal ric)
    {
        RelacaoInsulinaCarboidrato = ric;
        EstadoCadastro = EstadoCadastro.AguardandoFS;
    }

    public void DefinirFatorSensibilidade(decimal fs)
    {
        FatorSensibilidade = fs;
        EstadoCadastro = EstadoCadastro.AguardandoGlicemiaAlvo;
    }

    public void DefinirGlicemiaAlvo(int glicemiaAlvo)
    {
        GlicemiaAlvo = glicemiaAlvo;
        EstadoCadastro = EstadoCadastro.Registrado;
    }

    public void AtualizarParametros(decimal ric, decimal fs, int glicemiaAlvo)
    {
        RelacaoInsulinaCarboidrato = ric;
        FatorSensibilidade = fs;
        GlicemiaAlvo = glicemiaAlvo;
    }

    public void Desativar() => Ativo = false;
    public void Ativar() => Ativo = true;
}
