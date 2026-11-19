using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Esclari.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class TiposDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "tipo_usuario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instituicao_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    sistema = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipo_usuario", x => x.id);
                    table.ForeignKey(
                        name: "fk_tipo_usuario_instituicao_instituicao_id",
                        column: x => x.instituicao_id,
                        principalTable: "instituicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tipo_usuario_funcao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instituicao_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    funcao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipo_usuario_funcao", x => x.id);
                    table.ForeignKey(
                        name: "fk_tipo_usuario_funcao_instituicoes_instituicao_id",
                        column: x => x.instituicao_id,
                        principalTable: "instituicao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tipo_usuario_funcao_tipos_de_usuario_tipo_usuario_id",
                        column: x => x.tipo_usuario_id,
                        principalTable: "tipo_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "ix_tipo_usuario_instituicao_id_sistema",
                table: "tipo_usuario",
                columns: new[] { "instituicao_id", "sistema" },
                unique: true,
                filter: "sistema is not null");

            migrationBuilder.CreateIndex(
                name: "ix_tipo_usuario_funcao_instituicao_id",
                table: "tipo_usuario_funcao",
                column: "instituicao_id");

            migrationBuilder.CreateIndex(
                name: "ix_tipo_usuario_funcao_tipo_usuario_id_funcao",
                table: "tipo_usuario_funcao",
                columns: new[] { "tipo_usuario_id", "funcao" },
                unique: true);

            migrationBuilder.AddColumn<long>(
                name: "tipo_usuario_id",
                table: "usuario",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_tipo_usuario_nome ON tipo_usuario (instituicao_id, lower(nome));");

            migrationBuilder.Sql(
                """
                INSERT INTO tipo_usuario (instituicao_id, nome, sistema)
                SELECT i.id, t.nome, t.sistema FROM instituicao i
                CROSS JOIN (VALUES ('Aluno', 'ALUNO'), ('Gestor', 'GESTOR'), ('Atendente', 'ATENDENTE')) AS t (nome, sistema);

                INSERT INTO tipo_usuario (instituicao_id, nome, sistema)
                SELECT DISTINCT u.instituicao_id, 'Líder de semestre', NULL
                FROM usuario u
                JOIN instituicao i ON i.id = u.instituicao_id
                WHERE EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ALUNO')
                  AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ATENDENTE')
                  AND NOT EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR');

                INSERT INTO tipo_usuario_funcao (instituicao_id, tipo_usuario_id, funcao)
                SELECT t.instituicao_id, t.id, f.funcao FROM tipo_usuario t
                CROSS JOIN (VALUES ('fila.atender'), ('indicadores.ver'), ('mcp.usar')) AS f (funcao)
                WHERE t.sistema = 'ATENDENTE';

                INSERT INTO tipo_usuario_funcao (instituicao_id, tipo_usuario_id, funcao)
                SELECT t.instituicao_id, t.id, f.funcao FROM tipo_usuario t
                CROSS JOIN (VALUES ('assistente.usar'), ('fila.atender'), ('indicadores.ver'), ('mcp.usar')) AS f (funcao)
                WHERE t.sistema IS NULL AND t.nome = 'Líder de semestre';

                DO $$
                DECLARE r record;
                BEGIN
                    FOR r IN
                        SELECT u.instituicao_id, count(*) AS total FROM usuario u
                        WHERE EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ALUNO')
                          AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR')
                        GROUP BY u.instituicao_id
                    LOOP
                        RAISE NOTICE 'Instituição %: % usuário(s) com ALUNO e ADMINISTRADOR passam a GESTOR e perdem o uso do assistente.', r.instituicao_id, r.total;
                    END LOOP;
                END $$;

                UPDATE usuario u SET tipo_usuario_id = (
                    SELECT t.id FROM tipo_usuario t
                    WHERE t.instituicao_id = u.instituicao_id
                      AND CASE
                            WHEN EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ADMINISTRADOR') THEN t.sistema = 'GESTOR'
                            WHEN EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ALUNO')
                             AND EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ATENDENTE') THEN t.sistema IS NULL AND t.nome = 'Líder de semestre'
                            WHEN EXISTS (SELECT 1 FROM usuario_papel p WHERE p.usuario_id = u.id AND p.papel = 'ATENDENTE') THEN t.sistema = 'ATENDENTE'
                            ELSE t.sistema = 'ALUNO'
                          END);
                """);

            migrationBuilder.AlterColumn<long>(
                name: "tipo_usuario_id",
                table: "usuario",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_tipo_usuario_id",
                table: "usuario",
                column: "tipo_usuario_id");
            migrationBuilder.AddForeignKey(
                name: "fk_usuario_tipo_usuario_tipo_usuario_id",
                table: "usuario",
                column: "tipo_usuario_id",
                principalTable: "tipo_usuario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuario_tipo_usuario_tipo_usuario_id",
                table: "usuario");

            migrationBuilder.DropTable(
                name: "tipo_usuario_funcao");

            migrationBuilder.DropTable(
                name: "tipo_usuario");

            migrationBuilder.DropIndex(
                name: "ix_usuario_tipo_usuario_id",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "tipo_usuario_id",
                table: "usuario");
        }
    }
}
