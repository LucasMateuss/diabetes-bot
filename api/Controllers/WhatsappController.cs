using DiabetesBot.Models.WhatsApp;
using DiabetesBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiabetesBot.Controllers;

[ApiController]
[Route("webhook/whatsapp")]
public class WhatsappController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsappController> _logger;

    public WhatsappController(
        IServiceScopeFactory scopeFactory,
        ILogger<WhatsappController> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult ReceiveWebhook([FromBody] EvolutionWebhookPayload? payload)
    {
        if (payload?.Event != "messages.upsert" || payload.Data == null)
            return Ok();

        if (payload.Data.Key.FromMe || payload.Data.Key.RemoteJid.Contains("@g.us"))
            return Ok();

        var phoneNumber = payload.Data.Key.RemoteJid.Replace("@s.whatsapp.net", "");
        var text = payload.Data.Message?.Conversation
                   ?? payload.Data.Message?.ExtendedTextMessage?.Text;

        if (string.IsNullOrWhiteSpace(text))
            return Ok();

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IBotOrchestratorService>();
                await orchestrator.ProcessMessageAsync(phoneNumber, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar mensagem recebida de {Phone}.", phoneNumber);
            }
        });

        return Ok();
    }
}

