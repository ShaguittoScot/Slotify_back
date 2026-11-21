using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slotify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessPhysicalLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "calle",
                table: "negocios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ciudad",
                table: "negocios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_pais",
                table: "negocios",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                defaultValue: "MX");

            migrationBuilder.AddColumn<string>(
                name: "codigo_postal",
                table: "negocios",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "colonia",
                table: "negocios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "negocios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "negocios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_exterior",
                table: "negocios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_interior",
                table: "negocios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "calle",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "ciudad",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "codigo_pais",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "codigo_postal",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "colonia",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "numero_exterior",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "numero_interior",
                table: "negocios");
        }
    }
}
