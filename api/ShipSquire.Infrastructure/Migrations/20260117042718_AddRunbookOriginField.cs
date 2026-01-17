using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipSquire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunbookOriginField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisSnapshot",
                table: "Runbooks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Runbooks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisSnapshot",
                table: "Runbooks");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Runbooks");
        }
    }
}
