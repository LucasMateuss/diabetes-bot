using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using DiabetesBot.Data;
using DiabetesBot.Entities;
using DiabetesBot.Enums;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DiabetesBot.Services;

public class BotOrchestratorService : IBotOrchestratorService
{
    private readonly AppDbContext _dbContext;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IGeminiService _geminiService;
    private readonly INutritionService _nutritionService;
    private readonly IInsulinCalculatorService _insulinCalculator;

    private static readonly ConcurrentDictionary<string, PendingMeal> PendingMeals = new();

    public BotOrchestratorService(
        AppDbContext dbContext,
        IWhatsAppService whatsAppService,
        IGeminiService geminiService,
        INutritionService nutritionService,
        IInsulinCalculatorService insulinCalculator)
    {
        _dbContext = dbContext;
        _whatsAppService = whatsAppService;
        _geminiService = geminiService;
        _nutritionService = nutritionService;
        _insulinCalculator = insulinCalculator;
    }

    public async Task ProcessMessageAsync(string phoneNumber, string message)
    {
        var normalizedMessage = message.Trim().ToLowerInvariant();

        var paciente = await _dbContext.Pacientes
            .FirstOrDefaultAsync(p => p.TelefoneWhatsApp == phoneNumber);

        if (PendingMeals.ContainsKey(phoneNumber) && paciente != null)
        {
            await HandlePendingGlycemiaAsync(phoneNumber, normalizedMessage, paciente);
            return;
        }

        if (paciente == null)
        {
            paciente = new Paciente(phoneNumber);
            _dbContext.Pacientes.Add(paciente);
            await _dbContext.SaveChangesAsync();

            await _whatsAppService.SendTextMessageAsync(phoneNumber,
                "*Olá! Bem-vindo ao DiabetesBot!*\n\n" +
                "Sou uma calculadora de insulina que ajuda na contagem de carboidratos.\n\n" +
                "*Aviso importante:* Este bot é uma ferramenta auxiliar. " +
                "Os parâmetros de cálculo devem ser fornecidos pelo seu médico endocrinologista.\n\n" +
                "Vamos começar o cadastro!\n" +
                "Por favor, me diga seu *nome*:");
            return;
        }

        if (paciente.EstadoCadastro != EstadoCadastro.Registrado)
        {
            await HandleRegistrationFlowAsync(paciente, message);
            return;
        }

        await HandleRegisteredUserAsync(paciente, normalizedMessage, message);
    }

    private async Task HandleRegistrationFlowAsync(Paciente paciente, string message)
    {
        var phone = paciente.TelefoneWhatsApp;

        switch (paciente.EstadoCadastro)
        {
            case EstadoCadastro.AguardandoNome:
                paciente.DefinirNome(message.Trim());
                await _dbContext.SaveChangesAsync();

                await _whatsAppService.SendTextMessageAsync(phone,
                    $"Nome registrado: *{message.Trim()}*\n\n" +
                    "Agora preciso dos seus parâmetros clínicos (fornecidos pelo seu médico).\n\n" +
                    "Qual é a sua *Relação Insulina-Carboidrato (RIC)*?\n" +
                    "_(Quantas gramas de carboidrato 1 unidade de insulina cobre)_\n\n" +
                    "Exemplo: se 1U cobre 15g de carbo, digite *15*");
                break;

            case EstadoCadastro.AguardandoRIC:
                if (!TryParsePositiveDecimal(message, out var ric))
                {
                    await _whatsAppService.SendTextMessageAsync(phone,
                        "Valor inválido. Por favor, digite um número positivo.\n" +
                        "Exemplo: *15* (se 1U de insulina cobre 15g de carbo)");
                    return;
                }

                paciente.DefinirRelacaoInsulinaCarboidrato(ric);
                await _dbContext.SaveChangesAsync();

                await _whatsAppService.SendTextMessageAsync(phone,
                    $"RIC registrado: *1U para cada {ric}g de carboidrato*\n\n" +
                    "Qual é o seu *Fator de Sensibilidade (FS)*?\n" +
                    "_(Quantos mg/dL 1 unidade de insulina reduz sua glicemia)_\n\n" +
                    "Exemplo: se 1U reduz 50 mg/dL, digite *50*");
                break;

            case EstadoCadastro.AguardandoFS:
                if (!TryParsePositiveDecimal(message, out var fs))
                {
                    await _whatsAppService.SendTextMessageAsync(phone,
                        "Valor inválido. Por favor, digite um número positivo.\n" +
                        "Exemplo: *50* (se 1U reduz 50 mg/dL)");
                    return;
                }

                paciente.DefinirFatorSensibilidade(fs);
                await _dbContext.SaveChangesAsync();

                await _whatsAppService.SendTextMessageAsync(phone,
                    $"Fator de Sensibilidade registrado: *1U reduz {fs} mg/dL*\n\n" +
                    "Qual é a sua *meta de glicemia alvo* (em mg/dL)?\n" +
                    "_(Valor alvo definido pelo seu médico)_\n\n" +
                    "Exemplo: *120*");
                break;

            case EstadoCadastro.AguardandoGlicemiaAlvo:
                if (!int.TryParse(message.Trim(), out var glicemiaAlvo) || glicemiaAlvo <= 0)
                {
                    await _whatsAppService.SendTextMessageAsync(phone,
                        "Valor inválido. Por favor, digite um número inteiro positivo.\n" +
                        "Exemplo: *120*");
                    return;
                }

                paciente.DefinirGlicemiaAlvo(glicemiaAlvo);
                await _dbContext.SaveChangesAsync();

                await _whatsAppService.SendTextMessageAsync(phone,
                    "*Cadastro completo!*\n\n" +
                    "Seus dados:\n" +
                    $"- Nome: {paciente.Nome}\n" +
                    $"- RIC: 1U para {paciente.RelacaoInsulinaCarboidrato}g de carbo\n" +
                    $"- FS: 1U reduz {paciente.FatorSensibilidade} mg/dL\n" +
                    $"- Meta glicêmica: {paciente.GlicemiaAlvo} mg/dL\n\n" +
                    "Agora é só me dizer o que vai comer!\n" +
                    "Exemplo: _\"3 colheres de arroz, feijão e um bife\"_\n\n" +
                    "Digite *ajuda* para ver todos os comandos.");
                break;
        }
    }

    private async Task HandleRegisteredUserAsync(Paciente paciente, string normalizedMessage, string originalMessage)
    {
        var phone = paciente.TelefoneWhatsApp;

        switch (normalizedMessage)
        {
            case "ajuda":
            case "help":
            case "menu":
                await SendHelpMessageAsync(phone);
                return;

            case "meus dados":
            case "dados":
            case "perfil":
                await SendProfileAsync(phone, paciente);
                return;

            case "historico":
            case "histórico":
                await SendHistoryAsync(phone, paciente);
                return;

            case "atualizar":
            case "editar":
                await StartUpdateFlowAsync(phone, paciente);
                return;
        }

        await ProcessMealAsync(paciente, originalMessage);
    }

    private async Task ProcessMealAsync(Paciente paciente, string mealDescription)
    {
        var phone = paciente.TelefoneWhatsApp;

        await _whatsAppService.SendTextMessageAsync(phone,
            "*Analisando sua refeição...*\nIsso pode levar alguns segundos.");

        var foodItems = await _geminiService.ParseFoodDescriptionAsync(mealDescription);

        if (foodItems.Count == 0)
        {
            await _whatsAppService.SendTextMessageAsync(phone,
                "Não consegui identificar os alimentos na sua descrição.\n" +
                "Tente novamente com mais detalhes.\n\n" +
                "Exemplo: _\"100g de arroz branco, 2 colheres de feijão e um filé de frango grelhado\"_");
            return;
        }

        var nutritionResults = await _nutritionService.SearchFoodsAsync(foodItems);
        var notFound = nutritionResults.Where(r => !r.Encontrado).ToList();

        PendingMeals[phone] = new PendingMeal
        {
            MealDescription = mealDescription,
            FoodItems = foodItems,
            NutritionResults = nutritionResults
        };

        var preview = "*Alimentos identificados:*\n";
        foreach (var result in nutritionResults)
        {
            var portionLabel = FormatPortionLabel(result);
            if (result.Encontrado)
            {
                preview += $"- *{result.NomeEncontrado}* ({portionLabel}): *{result.TotalCarboidratos:F1}g de carboidratos* _({result.CarboidratoPor100g:F1}g/100g | Fonte: {result.Fonte})_\n";
            }
            else
            {
                preview += $"[NÃO ENCONTRADO] {result.NomeEncontrado} ({portionLabel})\n";
            }
        }

        if (notFound.Count > 0)
        {
            preview += "\n_Alimentos não encontrados foram desconsiderados no cálculo._\n";
        }

        preview += "\nQual é sua *glicemia atual* (em mg/dL)?\n" +
                   "_(Ou digite *pular* se não quiser incluir correção)_";

        await _whatsAppService.SendTextMessageAsync(phone, preview);
    }

    private async Task HandlePendingGlycemiaAsync(string phoneNumber, string message, Paciente paciente)
    {
        if (!PendingMeals.TryRemove(phoneNumber, out var pending))
            return;

        int? glycemia = null;

        if (message is not ("pular" or "skip" or "não" or "nao"))
        {
            if (int.TryParse(message.Trim(), out var parsedGlycemia) && parsedGlycemia > 0)
            {
                glycemia = parsedGlycemia;
            }
            else
            {
                await _whatsAppService.SendTextMessageAsync(phoneNumber,
                    "Valor inválido. Calculando apenas o bolus da refeição.");
            }
        }

        var result = _insulinCalculator.CalculateTotalDose(
            pending.NutritionResults,
            paciente.RelacaoInsulinaCarboidrato,
            paciente.FatorSensibilidade,
            paciente.GlicemiaAlvo,
            glycemia);

        var registro = new RegistroRefeicao(
            paciente.Id,
            pending.MealDescription,
            JsonSerializer.Serialize(pending.FoodItems),
            result.TotalCarboidratos,
            result.DoseTotal,
            glycemia,
            pending.NutritionResults.FirstOrDefault(r => r.Encontrado)?.Fonte ?? FonteNutricional.TACO);

        _dbContext.RegistrosRefeicao.Add(registro);
        await _dbContext.SaveChangesAsync();

        var response = FormatInsulinResult(result);
        await _whatsAppService.SendTextMessageAsync(phoneNumber, response);
    }

    private static string FormatInsulinResult(InsulinResult result)
    {
        var msg = "*RESULTADO DO CÁLCULO*\n\n";

        msg += "*Refeição:*\n";
        foreach (var alimento in result.Alimentos.Where(a => a.Encontrado))
        {
            var portionLabel = FormatPortionLabel(alimento);
            msg += $"- *{alimento.NomeEncontrado}* ({portionLabel}): *{alimento.TotalCarboidratos:F1}g de carboidratos* _({alimento.CarboidratoPor100g:F1}g/100g - {alimento.Fonte})_\n";
        }

        msg += $"\n*Total de carboidratos:* {result.TotalCarboidratos:F1}g\n\n";
        msg += "*Bolus Refeição:*\n";
        msg += $"{result.TotalCarboidratos:F1}g / {result.RICUtilizada} (RIC) = *{result.BolusRefeicao:F1}U*\n";

        if (result.GlicemiaInformada.HasValue && result.BolusCorrecao.HasValue)
        {
            msg += "\n*Bolus Correção:*\n";
            msg += $"({result.GlicemiaInformada} - {result.GlicemiaAlvo}) / {result.FSUtilizado} (FS) = *{result.BolusCorrecao:F1}U*\n";
        }

        msg += $"\n*DOSE TOTAL: {result.DoseTotal:F1} unidades*\n\n";
        msg += "_Este cálculo é baseado nos parâmetros informados por você. Sempre confirme com seu médico._";

        return msg;
    }

    private static string FormatPortionLabel(NutritionResult item)
    {
        return !string.IsNullOrWhiteSpace(item.MedidaInformada)
            ? $"{item.MedidaInformada} / {item.QuantidadeGramas}g"
            : $"{item.QuantidadeGramas}g";
    }

    private async Task SendHelpMessageAsync(string phone)
    {
        await _whatsAppService.SendTextMessageAsync(phone,
            "*COMANDOS DO DIABETESBOT*\n\n" +
            "*Para calcular insulina:*\n" +
            "Basta descrever o que vai comer.\n" +
            "Ex: _\"arroz, feijão, bife e salada\"_\n" +
            "Ex: _\"2 fatias de pão com manteiga\"_\n" +
            "Ex: _\"100g de macarrão e 150g de frango\"_\n\n" +
            "*Comandos:*\n" +
            "- *meus dados* - ver seus parâmetros\n" +
            "- *atualizar* - alterar parâmetros\n" +
            "- *historico* - últimas refeições\n" +
            "- *ajuda* - exibir este menu\n\n" +
            "_Os parâmetros de cálculo devem ser definidos pelo seu endocrinologista._");
    }

    private async Task SendProfileAsync(string phone, Paciente paciente)
    {
        await _whatsAppService.SendTextMessageAsync(phone,
            "*SEUS DADOS*\n\n" +
            $"Nome: {paciente.Nome}\n" +
            $"RIC: 1U para {paciente.RelacaoInsulinaCarboidrato}g de carbo\n" +
            $"FS: 1U reduz {paciente.FatorSensibilidade} mg/dL\n" +
            $"Meta glicêmica: {paciente.GlicemiaAlvo} mg/dL\n" +
            $"Cadastro: {paciente.DataCadastro:dd/MM/yyyy}\n\n" +
            "Para alterar, digite *atualizar*.");
    }

    private async Task SendHistoryAsync(string phone, Paciente paciente)
    {
        var registros = await _dbContext.RegistrosRefeicao
            .Where(r => r.PacienteId == paciente.Id)
            .OrderByDescending(r => r.DataRegistro)
            .Take(5)
            .ToListAsync();

        if (registros.Count == 0)
        {
            await _whatsAppService.SendTextMessageAsync(phone,
                "Você ainda não tem refeições registradas.\n" +
                "Me diga o que vai comer para começar!");
            return;
        }

        var msg = "*ÚLTIMAS REFEIÇÕES*\n\n";
        foreach (var reg in registros)
        {
            msg += $"{reg.DataRegistro:dd/MM/yyyy HH:mm}\n";
            msg += $"_{reg.DescricaoOriginal}_\n";
            msg += $"Carbs: {reg.TotalCarboidratos:F1}g -> *{reg.DoseCalculada:F1}U*\n\n";
        }

        await _whatsAppService.SendTextMessageAsync(phone, msg);
    }

    private async Task StartUpdateFlowAsync(string phone, Paciente paciente)
    {
        paciente.DefinirNome(paciente.Nome);
        await _dbContext.SaveChangesAsync();

        await _whatsAppService.SendTextMessageAsync(phone,
            "*Vamos atualizar seus parâmetros.*\n\n" +
            "Qual é a sua nova *Relação Insulina-Carboidrato (RIC)*?\n" +
            "_(Quantas gramas de carboidrato 1 unidade de insulina cobre)_\n\n" +
            "Exemplo: *15*");
    }

    private static bool TryParsePositiveDecimal(string input, out decimal value)
    {
        return decimal.TryParse(
            input.Trim().Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out value) && value > 0;
    }

    private sealed class PendingMeal
    {
        public string MealDescription { get; init; } = string.Empty;
        public List<FoodItem> FoodItems { get; init; } = new();
        public List<NutritionResult> NutritionResults { get; init; } = new();
    }
}
