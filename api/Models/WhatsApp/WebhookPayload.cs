using System.Text.Json.Serialization;

namespace DiabetesBot.Models.WhatsApp;

public class EvolutionWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("instance")]
    public string Instance { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public EvolutionMessageData? Data { get; set; }
}

public class EvolutionMessageData
{
    [JsonPropertyName("key")]
    public EvolutionMessageKey Key { get; set; } = new();

    [JsonPropertyName("pushName")]
    public string? PushName { get; set; }

    [JsonPropertyName("message")]
    public EvolutionMessage? Message { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("messageTimestamp")]
    public long? MessageTimestamp { get; set; }
}

public class EvolutionMessageKey
{
    [JsonPropertyName("remoteJid")]
    public string RemoteJid { get; set; } = string.Empty;

    [JsonPropertyName("fromMe")]
    public bool FromMe { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class EvolutionMessage
{
    [JsonPropertyName("conversation")]
    public string? Conversation { get; set; }

    [JsonPropertyName("extendedTextMessage")]
    public ExtendedTextMessage? ExtendedTextMessage { get; set; }
}

public class ExtendedTextMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
