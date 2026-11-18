using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class OperadoresDaPlataforma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operador_plataforma",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    senha_hash = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    totp_segredo = table.Column<string>(type: "text", nullable: true),
                    totp_ultimo_passo = table.Column<long>(type: "bigint", nullable: true),
                    convite_hash = table.Column<string>(type: "char(64)", nullable: true),
                    convite_expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operador_plataforma", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auditoria_plataforma",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    operador_id = table.Column<long>(type: "bigint", nullable: false),
                    instituicao_alvo_id = table.Column<long>(type: "bigint", nullable: true),
                    resumo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditoria_plataforma", x => x.id);
                    table.ForeignKey(
                        name: "fk_auditoria_plataforma_instituicoes_instituicao_alvo_id",
                        column: x => x.instituicao_alvo_id,
                        principalTable: "instituicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_auditoria_plataforma_operadores_plataforma_operador_id",
                        column: x => x.operador_id,
                        principalTable: "operador_plataforma",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_criada_em",
                table: "auditoria_plataforma",
                column: "criada_em");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_instituicao_alvo_id",
                table: "auditoria_plataforma",
                column: "instituicao_alvo_id");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_plataforma_operador_id",
                table: "auditoria_plataforma",
                column: "operador_id");

            migrationBuilder.CreateIndex(
                name: "ix_operador_plataforma_convite_hash",
                table: "operador_plataforma",
                column: "convite_hash",
                unique: true);

            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_operador_plataforma_email ON operador_plataforma (lower(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_plataforma");

            migrationBuilder.DropTable(
                name: "operador_plataforma");
        }
    }
}
