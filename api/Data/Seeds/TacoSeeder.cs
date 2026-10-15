using System.Text.Json;
using DiabetesBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiabetesBot.Data.Seeds;

public static class TacoSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Alimentos.AnyAsync())
        {
            await dbContext.Alimentos
                .Where(a => (a.CodigoTaco == 457 || a.CodigoTaco == 458) && a.CarboidratoPor100g == 0)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CarboidratoPor100g, 4.9m));

            await dbContext.Alimentos
                .Where(a => a.CodigoTaco == 450 && a.CarboidratoPor100g == 0)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CarboidratoPor100g, 11.5m));

            await dbContext.Alimentos
                .Where(a => a.CodigoTaco == 591 && a.CarboidratoPor100g == 0)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CarboidratoPor100g, 9.4m));

            await dbContext.Alimentos
                .Where(a => a.CarboidratoPor100g < 0)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.CarboidratoPor100g, 0m));

            return;
        }

        var jsonPath = ResolveSeedFilePath();
        if (jsonPath == null)
        {
            logger.LogWarning("Arquivo taco_data.json não encontrado. Seed da tabela TACO ignorado.");
            return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var tacoItems = JsonSerializer.Deserialize<List<TacoItem>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tacoItems == null || tacoItems.Count == 0)
        {
            logger.LogWarning("Arquivo taco_data.json está vazio ou inválido.");
            return;
        }

        var alimentos = tacoItems.Select(item => new Alimento(
            codigoTaco: item.Codigo,
            nome: item.Nome,
            grupo: item.Grupo,
            carboidratoPor100g: item.Carboidrato,
            proteinaPor100g: item.Proteina,
            lipidioPor100g: item.Lipidio,
            fibraPor100g: item.Fibra,
            energiaPor100g: item.Energia
        )).ToList();

        await dbContext.Alimentos.AddRangeAsync(alimentos);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Tabela TACO populada com {Count} alimentos.", alimentos.Count);
    }

    private static string? ResolveSeedFilePath()
    {
        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, "Data", "Seeds", "taco_data.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seeds", "taco_data.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "Seeds", "taco_data.json")
        ];

        return candidatePaths.FirstOrDefault(File.Exists);
    }

    private sealed class TacoItem
    {
        public int Codigo { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public decimal Carboidrato { get; set; }
        public decimal Proteina { get; set; }
        public decimal Lipidio { get; set; }
        public decimal Fibra { get; set; }
        public decimal Energia { get; set; }
    }
}
