using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IngestionService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSymmetricKeyToSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SymmetricKey",
                table: "Sensors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SymmetricKey",
                table: "Sensors");
        }
    }
}
