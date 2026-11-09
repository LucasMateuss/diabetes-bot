using DiabetesBot.Entities;
using DiabetesBot.Enums;
using FluentAssertions;
using Xunit;

namespace DiabetesBot.UnitTests.Entities;

public class PacienteTests
{
    [Fact]
    public void Constructor_InitializesWithExpectedDefaults()
    {
        var phone = "5511999998888";
        var paciente = new Paciente(phone);

        paciente.Id.Should().NotBeEmpty();
        paciente.TelefoneWhatsApp.Should().Be(phone);
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.AguardandoNome);
        paciente.Ativo.Should().BeTrue();
        paciente.Nome.Should().BeEmpty();
        paciente.DataCadastro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RegistrationFlow_StepByStep_TransitionsStatesAndSetsPropertiesCorrectly()
    {
        var paciente = new Paciente("5511999998888");
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.AguardandoNome);

        paciente.DefinirNome("Lucas");
        paciente.Nome.Should().Be("Lucas");
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.AguardandoRIC);

        paciente.DefinirRelacaoInsulinaCarboidrato(15m);
        paciente.RelacaoInsulinaCarboidrato.Should().Be(15m);
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.AguardandoFS);

        paciente.DefinirFatorSensibilidade(50m);
        paciente.FatorSensibilidade.Should().Be(50m);
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.AguardandoGlicemiaAlvo);

        paciente.DefinirGlicemiaAlvo(120);
        paciente.GlicemiaAlvo.Should().Be(120);
        paciente.EstadoCadastro.Should().Be(EstadoCadastro.Registrado);
    }

    [Fact]
    public void AtualizarParametros_UpdatesClinicalValuesDirectly()
    {
        var paciente = new Paciente("5511999998888");
        paciente.DefinirNome("Lucas");
        paciente.DefinirRelacaoInsulinaCarboidrato(15m);
        paciente.DefinirFatorSensibilidade(50m);
        paciente.DefinirGlicemiaAlvo(120);

        paciente.AtualizarParametros(ric: 12m, fs: 45m, glicemiaAlvo: 110);

        paciente.RelacaoInsulinaCarboidrato.Should().Be(12m);
        paciente.FatorSensibilidade.Should().Be(45m);
        paciente.GlicemiaAlvo.Should().Be(110);
    }

    [Fact]
    public void DesativarAndAtivar_TogglesActiveState()
    {
        var paciente = new Paciente("5511999998888");
        paciente.Ativo.Should().BeTrue();

        paciente.Desativar();
        paciente.Ativo.Should().BeFalse();

        paciente.Ativar();
        paciente.Ativo.Should().BeTrue();
    }
}
