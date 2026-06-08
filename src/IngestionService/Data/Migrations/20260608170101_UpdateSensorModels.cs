using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IngestionService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSensorModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "Sensors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Sensors");
        }
    }
}
