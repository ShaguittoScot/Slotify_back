using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slotify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBusinessRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios_negocios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_negocio = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    esta_activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_negocios", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_negocios_negocios_id_negocio",
                        column: x => x.id_negocio,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuarios_negocios_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint(
                        name: "CK_usuarios_negocios_rol",
                        sql: "rol IN ('DUENO', 'EMPLEADO')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_negocios_id_negocio",
                table: "usuarios_negocios",
                column: "id_negocio");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_negocios_id_usuario_id_negocio",
                table: "usuarios_negocios",
                columns: new[] { "id_usuario", "id_negocio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios_negocios");
        }
    }
}
