using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiabetesBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoTaco = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Grupo = table.Column<string>(type: "text", nullable: false),
                    CarboidratoPor100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ProteinaPor100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LipidioPor100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FibraPor100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EnergiaPor100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    TelefoneWhatsApp = table.Column<string>(type: "text", nullable: false),
                    FatorSensibilidade = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    RelacaoInsulinaCarboidrato = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    GlicemiaAlvo = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    EstadoCadastro = table.Column<int>(type: "integer", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosRefeicao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DescricaoOriginal = table.Column<string>(type: "text", nullable: false),
                    AlimentosJson = table.Column<string>(type: "text", nullable: false),
                    TotalCarboidratos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DoseCalculada = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    GlicemiaInformada = table.Column<int>(type: "integer", nullable: true),
                    FonteNutricional = table.Column<int>(type: "integer", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosRefeicao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosRefeicao_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_CodigoTaco",
                table: "Alimentos",
                column: "CodigoTaco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_Nome",
                table: "Alimentos",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TelefoneWhatsApp",
                table: "Pacientes",
                column: "TelefoneWhatsApp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosRefeicao_DataRegistro",
                table: "RegistrosRefeicao",
                column: "DataRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosRefeicao_PacienteId",
                table: "RegistrosRefeicao",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alimentos");

            migrationBuilder.DropTable(
                name: "RegistrosRefeicao");

            migrationBuilder.DropTable(
                name: "Pacientes");
        }
    }
}
