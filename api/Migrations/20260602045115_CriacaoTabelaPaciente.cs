using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiabetesBot.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoTabelaPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    TelefoneWhatsApp = table.Column<string>(type: "text", nullable: false),
                    FatorSensibilidade = table.Column<decimal>(type: "numeric", nullable: false),
                    RelacaoInsulinaCarboidrato = table.Column<decimal>(type: "numeric", nullable: false),
                    GlicemiaAlvo = table.Column<int>(type: "integer", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_TelefoneWhatsApp",
                table: "Pacientes",
                column: "TelefoneWhatsApp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pacientes");
        }
    }
}
