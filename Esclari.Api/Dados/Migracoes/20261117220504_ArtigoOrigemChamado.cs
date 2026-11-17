using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ArtigoOrigemChamado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "origem_chamado_id",
                table: "artigo",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_artigo_origem_chamado_id",
                table: "artigo",
                column: "origem_chamado_id",
                unique: true,
                filter: "origem_chamado_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_artigo_chamados_origem_chamado_id",
                table: "artigo",
                column: "origem_chamado_id",
                principalTable: "chamado",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_artigo_chamados_origem_chamado_id",
                table: "artigo");

            migrationBuilder.DropIndex(
                name: "ix_artigo_origem_chamado_id",
                table: "artigo");

            migrationBuilder.DropColumn(
                name: "origem_chamado_id",
                table: "artigo");
        }
    }
}
