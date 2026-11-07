using DiabetesBot.Data;
using DiabetesBot.Data.Seeds;
using DiabetesBot.Extensions;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Env.TraversePath().Load();

builder.Services
    .AddDatabase()
    .AddExternalIntegrations()
    .AddApplicationServices()
    .AddOpenApi()
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (app.Environment.IsDevelopment())
    {
        await dbContext.Database.MigrateAsync();
    }

    await TacoSeeder.SeedAsync(dbContext, logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
