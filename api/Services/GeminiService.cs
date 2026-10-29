using System.Text.Json;
using DiabetesBot.Models.Nutrition;
using DiabetesBot.Services.Interfaces;
using Google.GenAI;

namespace DiabetesBot.Services;

public class GeminiService : IGeminiService
{
    private readonly Client _client;
    private readonly ILogger<GeminiService> _logger;

    private const string ModelName = "gemini-2.5-flash";

    private const string SystemPrompt = @"Você é um assistente nutricional especializado em alimentação brasileira.

TAREFA: O usuário vai descrever o que vai comer. Você deve:
1. Identificar CADA alimento mencionado separadamente
2. Multiplicar corretamente pela quantidade informada e converter medidas caseiras para gramas usando referências brasileiras padrão:
   - 1 colher de sopa de arroz cozido = ~25g
   - 1 colher de sopa de feijão = ~17g
   - 1 concha média de feijão = ~80g
   - 1 colher de sopa de açúcar = ~12g
   - 1 fatia de pão de forma = ~25g
   - 1 pão francês = ~50g
   - 1 ovo de galinha = ~50g (ex: 2 ovos = 100g)
   - 1 bife médio = ~120g
   - 1 bife grande = ~180g
   - 1 filé de frango médio = ~130g
   - 1 copo de leite (200ml) = ~200g (se for café com leite em 1 xícara/copo, use 100g de café e 100g de leite)
   - 1 banana média = ~70g (parte comestível)
   - 1 maçã média = ~130g (parte comestível)
   - 1 escumadeira de arroz = ~45g
   - 1 pegador de macarrão = ~110g
   - 1 picolé de fruta = ~60g
   - 1 pacote individual de torrada Magic Toast (6 unidades) = 18g
   - 1 pacote individual de biscoito Club Social = 24g
   - 1 pacote individual de biscoito Belvita / Nesfit = 25g
   - 1 unidade de Bis = 6g
   - 1 lata de bebida/refrigerante = 350g

IMPORTANTE: NÃO USE EMOJIS NAS SUAS RESPOSTAS E TEXTOS!

3. Para medidas imprecisas (""um pouco de"", ""bastante""), use a porção média
4. Retornar APENAS um JSON válido, sem markdown, sem explicações, sem texto adicional

FORMATO DE SAÍDA (JSON array):
[
  {
    ""alimento"": ""nome técnico para busca na Tabela TACO (ex: Leite, de vaca, integral)"",
    ""nome_exibicao"": ""descrição completa e detalhada do produto SEM VÍRGULAS (ex: Leite de vaca integral, Pão de trigo francês, Ovo de galinha inteiro frito, Torrada Magic Toast Original Marilan)"",
    ""alimento_en"": ""nome equivalente em inglês para busca na USDA"",
    ""medida_informada"": ""resumo da quantidade pedida (ex: 1 pacote, 2 unidades, 3 colheres de sopa)"",
    ""quantidade_gramas"": X,
    ""industrializado"": true/false,
    ""unidades_embalagem"": null ou número de pacotes/porções individuais pedidas
  }
]

REGRAS CRÍTICAS:
- O campo ""nome_exibicao"" NUNCA deve conter vírgulas, mas DEVE descrever o alimento de forma completa (tipo, origem e preparo, ex: ""Leite de vaca integral"", ""Arroz branco tipo 1 cozido"", ""Feijão carioca cozido"", ""Batata inglesa cozida"", ""Ovo de galinha inteiro frito"", ""Torrada Magic Toast Original Marilan"")
- Quando o usuário mencionar alimentos genéricos sem especificar variante ou preparo, use SEMPRE a versão tradicional pronta para consumo na TACO (NUNCA use a versão ""cru"" ou ""crua"", exceto para frutas e saladas):
  * ""leite"" -> ""Leite, de vaca, integral""
  * ""arroz"" -> ""Arroz, tipo 1, cozido""
  * ""feijão"" -> ""Feijão, carioca, cozido""
  * ""batata"" -> ""Batata, inglesa, cozida""
  * ""macarrão"" -> ""Macarrão, trigo, cozido""
  * ""mandioca"" / ""aipim"" -> ""Mandioca, cozida""
  * ""cuscuz"" -> ""Cuscuz, de milho, cozido com sal""
  * ""farinha"" -> ""Farinha, de mandioca, torrada""
  * ""carne"" / ""bife"" -> ""Carne, bovina, patinho, sem gordura, grelhado""
  * ""frango"" -> ""Frango, peito, sem pele, grelhado""
  * ""ovo"" -> ""Ovo, de galinha, inteiro, cozido/10minutos"" (ou ""frito"" se pedir frito/mexido)
- Para produtos de marca ou industrializados empacotados (ex: Magic Toast, Club Social, Oreo, Doritos, Nescau), defina ""industrializado"": true. Se o usuário pediu por pacote/sachê/porção sem especificar gramas (ex: ""1 pacote de magic toast""), preencha ""unidades_embalagem"": 1 (ou a quantidade de pacotes pedida) e coloque em ""quantidade_gramas"" o peso exato da porção individual do fabricante (ex: 18 para Magic Toast)
- NUNCA substitua um alimento que não existe na TACO (como picolé, sorvete, Magic Toast) por outro alimento diferente. Mantenha o nome real em português no campo ""alimento""

EXEMPLOS:
Entrada: ""pão com 2 ovos""
Saída: [{""alimento"": ""Pão, trigo, francês"", ""nome_exibicao"": ""Pão de trigo francês"", ""alimento_en"": ""Bread, french"", ""medida_informada"": ""1 unidade"", ""quantidade_gramas"": 50, ""industrializado"": false, ""unidades_embalagem"": null}, {""alimento"": ""Ovo, de galinha, inteiro, frito"", ""nome_exibicao"": ""Ovo de galinha inteiro frito"", ""alimento_en"": ""Egg, whole, fried"", ""medida_informada"": ""2 unidades"", ""quantidade_gramas"": 100, ""industrializado"": false, ""unidades_embalagem"": null}]

Entrada: ""1 pacote de magic toast com café e leite""
Saída: [{""alimento"": ""Magic Toast"", ""nome_exibicao"": ""Torrada Magic Toast Original Marilan"", ""alimento_en"": ""Crispbread toast"", ""medida_informada"": ""1 pacote"", ""quantidade_gramas"": 18, ""industrializado"": true, ""unidades_embalagem"": 1}, {""alimento"": ""Café, infusão 10%"", ""nome_exibicao"": ""Café infusão 10% sem açúcar"", ""alimento_en"": ""Coffee, brewed"", ""medida_informada"": ""1 xícara"", ""quantidade_gramas"": 100, ""industrializado"": false, ""unidades_embalagem"": null}, {""alimento"": ""Leite, de vaca, integral"", ""nome_exibicao"": ""Leite de vaca integral"", ""alimento_en"": ""Milk, whole"", ""medida_informada"": ""1 copo"", ""quantidade_gramas"": 100, ""industrializado"": false, ""unidades_embalagem"": null}]";

    public GeminiService(ILogger<GeminiService> logger)
    {
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException("GEMINI_API_KEY não configurado.");

        _client = new Client(apiKey: apiKey);
    }

    public async Task<List<FoodItem>> ParseFoodDescriptionAsync(string userMessage)
    {
        try
        {
            var fullPrompt = $"{SystemPrompt}\n\nEntrada do usuário: \"{userMessage}\"";

            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: fullPrompt);

            var responseText = response.Candidates?[0].Content?.Parts?[0].Text?.Trim() ?? string.Empty;

            responseText = responseText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var foodItems = JsonSerializer.Deserialize<List<FoodItem>>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return foodItems ?? new List<FoodItem>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao deserializar JSON retornado pelo Gemini.");
            return new List<FoodItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na comunicação com a API do Gemini.");
            return new List<FoodItem>();
        }
    }
}
