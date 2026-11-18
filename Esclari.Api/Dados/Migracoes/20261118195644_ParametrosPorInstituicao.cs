using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ParametrosPorInstituicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "prazo_abandono_minutos",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_confirmacao_dias",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_lembrete_aguardando_dias",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_primeira_resposta_horas",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_resolucao_horas",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_resolver_aguardando_dias",
                table: "instituicao",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "prazo_validade_conhecimento_meses",
                table: "instituicao",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prazo_abandono_minutos",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_confirmacao_dias",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_lembrete_aguardando_dias",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_primeira_resposta_horas",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_resolucao_horas",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_resolver_aguardando_dias",
                table: "instituicao");

            migrationBuilder.DropColumn(
                name: "prazo_validade_conhecimento_meses",
                table: "instituicao");
        }
    }
}
