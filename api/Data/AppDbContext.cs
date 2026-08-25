using DiabetesBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiabetesBot.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Paciente> Pacientes { get; set; }
    public DbSet<Alimento> Alimentos { get; set; }
    public DbSet<RegistroRefeicao> RegistrosRefeicao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TelefoneWhatsApp).IsUnique();
            entity.Property(p => p.FatorSensibilidade).HasPrecision(10, 2);
            entity.Property(p => p.RelacaoInsulinaCarboidrato).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Alimento>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CodigoTaco).IsUnique();
            entity.HasIndex(a => a.Nome);

            entity.Property(a => a.CarboidratoPor100g).HasPrecision(10, 2);
            entity.Property(a => a.ProteinaPor100g).HasPrecision(10, 2);
            entity.Property(a => a.LipidioPor100g).HasPrecision(10, 2);
            entity.Property(a => a.FibraPor100g).HasPrecision(10, 2);
            entity.Property(a => a.EnergiaPor100g).HasPrecision(10, 2);
        });

        modelBuilder.Entity<RegistroRefeicao>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasOne(r => r.Paciente)
                .WithMany()
                .HasForeignKey(r => r.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.PacienteId);
            entity.HasIndex(r => r.DataRegistro);

            entity.Property(r => r.TotalCarboidratos).HasPrecision(10, 2);
            entity.Property(r => r.DoseCalculada).HasPrecision(10, 2);
        });
    }
}
