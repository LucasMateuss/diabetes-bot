using System.Text;
using System.Text.Json;
using DiabetesBot.Services.Interfaces;

namespace DiabetesBot.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly string _instanceName;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _instanceName = Environment.GetEnvironmentVariable("EVOLUTION_INSTANCE_NAME")
            ?? throw new InvalidOperationException("EVOLUTION_INSTANCE_NAME não configurado.");
    }

    public async Task SendTextMessageAsync(string toPhoneNumber, string message)
    {
        var cleanNumber = toPhoneNumber.Replace("@s.whatsapp.net", "");
        var url = $"/message/sendText/{_instanceName}";

        var payload = new
        {
            number = cleanNumber,
            text = message
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Falha ao enviar mensagem via Evolution API. Status: {StatusCode}, Body: {Body}",
                    response.StatusCode,
                    responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao enviar mensagem para {Phone}.", cleanNumber);
            throw;
        }
    }
}
