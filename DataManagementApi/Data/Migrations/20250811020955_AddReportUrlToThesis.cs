using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataManagementApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportUrlToThesis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportUrl",
                table: "Theses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportUrl",
                table: "Theses");
        }
    }
}
