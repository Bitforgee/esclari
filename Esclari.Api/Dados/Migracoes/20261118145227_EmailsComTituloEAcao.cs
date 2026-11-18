using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class EmailsComTituloEAcao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "acao_rotulo",
                table: "email_saida",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "acao_url",
                table: "email_saida",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "titulo",
                table: "email_saida",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acao_rotulo",
                table: "email_saida");

            migrationBuilder.DropColumn(
                name: "acao_url",
                table: "email_saida");

            migrationBuilder.DropColumn(
                name: "titulo",
                table: "email_saida");
        }
    }
}
