namespace DiabetesBot.Endpoints;

public static class WhatsappEndpoints
{
    public static void MapWhatsappEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/webhook/whatsapp");

        group.MapPost("/", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine("\n=== NOVA MENSAGEM RECEBIDA DO WHATSAPP ===");
            Console.WriteLine(body);
            Console.WriteLine("==========================================\n");

            return Results.Ok(new { status = "Mensagem processada com sucesso" });
        });
    }
}
