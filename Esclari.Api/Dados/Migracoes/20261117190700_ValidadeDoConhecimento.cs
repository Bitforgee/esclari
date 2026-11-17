using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class ValidadeDoConhecimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "valido_ate",
                table: "documento",
                type: "date",
                nullable: false,
                defaultValueSql: "(current_date + interval '12 months')::date");

            migrationBuilder.AddColumn<DateOnly>(
                name: "valido_ate",
                table: "artigo",
                type: "date",
                nullable: false,
                defaultValueSql: "(current_date + interval '12 months')::date");

            migrationBuilder.CreateTable(
                name: "aviso_validade",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instituicao_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    valido_ate = table.Column<DateOnly>(type: "date", nullable: false),
                    marco = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aviso_validade", x => x.id);
                    table.ForeignKey(
                        name: "fk_aviso_validade_instituicoes_instituicao_id",
                        column: x => x.instituicao_id,
                        principalTable: "instituicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aviso_validade_instituicao_id_tipo_item_id_valido_ate_marco",
                table: "aviso_validade",
                columns: new[] { "instituicao_id", "tipo", "item_id", "valido_ate", "marco" },
                unique: true);

            migrationBuilder.Sql("ALTER TABLE artigo ALTER COLUMN valido_ate DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE documento ALTER COLUMN valido_ate DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aviso_validade");

            migrationBuilder.DropColumn(
                name: "valido_ate",
                table: "documento");

            migrationBuilder.DropColumn(
                name: "valido_ate",
                table: "artigo");
        }
    }
}
