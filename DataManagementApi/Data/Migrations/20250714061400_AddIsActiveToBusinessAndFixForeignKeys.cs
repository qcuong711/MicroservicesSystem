using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataManagementApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToBusinessAndFixForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ThesisPeriods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ThesisPeriods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ThesisPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int?>(
                name: "AcademicYearId",
                table: "Theses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "SemesterId",
                table: "Theses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "AcademicYearId",
                table: "Internships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "SemesterId",
                table: "Internships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Internships",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InternshipPeriods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InternshipPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPartner",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    PartnersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartner", x => new { x.BusinessId, x.PartnersId });
                    table.ForeignKey(
                        name: "FK_BusinessPartner_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessPartner_Partners_PartnersId",
                        column: x => x.PartnersId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessThesis",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ThesesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessThesis", x => new { x.BusinessId, x.ThesesId });
                    table.ForeignKey(
                        name: "FK_BusinessThesis_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessThesis_Theses_ThesesId",
                        column: x => x.ThesesId,
                        principalTable: "Theses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessThesisPeriod",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ThesisPeriodsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessThesisPeriod", x => new { x.BusinessId, x.ThesisPeriodsId });
                    table.ForeignKey(
                        name: "FK_BusinessThesisPeriod_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessThesisPeriod_ThesisPeriods_ThesisPeriodsId",
                        column: x => x.ThesisPeriodsId,
                        principalTable: "ThesisPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerBusiness",
                columns: table => new
                {
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerBusiness", x => new { x.PartnerId, x.BusinessId });
                    table.ForeignKey(
                        name: "FK_PartnerBusiness_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerBusiness_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThesisPeriodBusiness",
                columns: table => new
                {
                    ThesisPeriodId = table.Column<int>(type: "int", nullable: false),
                    BusinessId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThesisPeriodBusiness", x => new { x.ThesisPeriodId, x.BusinessId });
                    table.ForeignKey(
                        name: "FK_ThesisPeriodBusiness_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Business",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThesisPeriodBusiness_ThesisPeriods_ThesisPeriodId",
                        column: x => x.ThesisPeriodId,
                        principalTable: "ThesisPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Theses_AcademicYearId",
                table: "Theses",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Theses_SemesterId",
                table: "Theses",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Internships_AcademicYearId",
                table: "Internships",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Internships_SemesterId",
                table: "Internships",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartner_PartnersId",
                table: "BusinessPartner",
                column: "PartnersId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessThesis_ThesesId",
                table: "BusinessThesis",
                column: "ThesesId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessThesisPeriod_ThesisPeriodsId",
                table: "BusinessThesisPeriod",
                column: "ThesisPeriodsId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerBusiness_BusinessId",
                table: "PartnerBusiness",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ThesisPeriodBusiness_BusinessId",
                table: "ThesisPeriodBusiness",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Internships_AcademicYears_AcademicYearId",
                table: "Internships",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Internships_Semesters_SemesterId",
                table: "Internships",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Theses_AcademicYears_AcademicYearId",
                table: "Theses",
                column: "AcademicYearId",
                principalTable: "AcademicYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Theses_Semesters_SemesterId",
                table: "Theses",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Internships_AcademicYears_AcademicYearId",
                table: "Internships");

            migrationBuilder.DropForeignKey(
                name: "FK_Internships_Semesters_SemesterId",
                table: "Internships");

            migrationBuilder.DropForeignKey(
                name: "FK_Theses_AcademicYears_AcademicYearId",
                table: "Theses");

            migrationBuilder.DropForeignKey(
                name: "FK_Theses_Semesters_SemesterId",
                table: "Theses");

            migrationBuilder.DropTable(
                name: "BusinessPartner");

            migrationBuilder.DropTable(
                name: "BusinessThesis");

            migrationBuilder.DropTable(
                name: "BusinessThesisPeriod");

            migrationBuilder.DropTable(
                name: "PartnerBusiness");

            migrationBuilder.DropTable(
                name: "ThesisPeriodBusiness");

            migrationBuilder.DropTable(
                name: "Business");

            migrationBuilder.DropIndex(
                name: "IX_Theses_AcademicYearId",
                table: "Theses");

            migrationBuilder.DropIndex(
                name: "IX_Theses_SemesterId",
                table: "Theses");

            migrationBuilder.DropIndex(
                name: "IX_Internships_AcademicYearId",
                table: "Internships");

            migrationBuilder.DropIndex(
                name: "IX_Internships_SemesterId",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ThesisPeriods");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ThesisPeriods");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InternshipPeriods");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ThesisPeriods",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InternshipPeriods",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
