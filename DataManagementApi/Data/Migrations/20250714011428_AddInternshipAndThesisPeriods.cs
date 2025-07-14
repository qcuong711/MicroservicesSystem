using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataManagementApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipAndThesisPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new tables for Periods
            migrationBuilder.CreateTable(
                name: "InternshipPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternshipPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternshipPeriods_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternshipPeriods_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThesisPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThesisPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThesisPeriods_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThesisPeriods_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Step 2: Add the new nullable columns to existing tables
            migrationBuilder.AddColumn<int>(
                name: "InternshipPeriodId",
                table: "Internships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThesisPeriodId",
                table: "Theses",
                type: "int",
                nullable: true);
                
            // Step 3: Seed data - Create default periods and update existing rows
            // This is a simplified approach. It assumes at least one AcademicYear and one Semester exist.
            migrationBuilder.Sql(@"
                -- Create a default Internship Period for existing data
                DECLARE @DefaultAcademicYearId INT = (SELECT TOP 1 Id FROM AcademicYears);
                DECLARE @DefaultSemesterId INT = (SELECT TOP 1 Id FROM Semesters);
                DECLARE @DefaultInternshipPeriodId INT;

                IF @DefaultAcademicYearId IS NOT NULL AND @DefaultSemesterId IS NOT NULL
                BEGIN
                    INSERT INTO InternshipPeriods (Name, Description, StartDate, EndDate, AcademicYearId, SemesterId, CreatedAt)
                    VALUES ('Legacy Internship Period', 'Default period for migrated internship data', '2000-01-01', '2000-01-01', @DefaultAcademicYearId, @DefaultSemesterId, GETUTCDATE());
                    SET @DefaultInternshipPeriodId = SCOPE_IDENTITY();
                    UPDATE Internships SET InternshipPeriodId = @DefaultInternshipPeriodId WHERE InternshipPeriodId IS NULL;
                END

                -- Create a default Thesis Period for existing data
                DECLARE @DefaultThesisPeriodId INT;
                IF @DefaultAcademicYearId IS NOT NULL AND @DefaultSemesterId IS NOT NULL
                BEGIN
                    INSERT INTO ThesisPeriods (Name, Description, StartDate, EndDate, AcademicYearId, SemesterId, CreatedAt)
                    VALUES ('Legacy Thesis Period', 'Default period for migrated thesis data', '2000-01-01', '2000-01-01', @DefaultAcademicYearId, @DefaultSemesterId, GETUTCDATE());
                    SET @DefaultThesisPeriodId = SCOPE_IDENTITY();
                    UPDATE Theses SET ThesisPeriodId = @DefaultThesisPeriodId WHERE ThesisPeriodId IS NULL;
                END
            ");

            // Step 4: Make the columns non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "InternshipPeriodId",
                table: "Internships",
                type: "int",
                nullable: false,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ThesisPeriodId",
                table: "Theses",
                type: "int",
                nullable: false,
                oldNullable: true);

            // Step 5: Create indexes and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_Internships_InternshipPeriodId",
                table: "Internships",
                column: "InternshipPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Theses_ThesisPeriodId",
                table: "Theses",
                column: "ThesisPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Internships_InternshipPeriods_InternshipPeriodId",
                table: "Internships",
                column: "InternshipPeriodId",
                principalTable: "InternshipPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Theses_ThesisPeriods_ThesisPeriodId",
                table: "Theses",
                column: "ThesisPeriodId",
                principalTable: "ThesisPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
                
            // Step 6: Drop the old foreign keys and columns
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

            migrationBuilder.DropIndex(
                name: "IX_Internships_AcademicYearId",
                table: "Internships");
                
            migrationBuilder.DropIndex(
                name: "IX_Internships_SemesterId",
                table: "Internships");

            migrationBuilder.DropIndex(
                name: "IX_Theses_AcademicYearId",
                table: "Theses");
                
            migrationBuilder.DropIndex(
                name: "IX_Theses_SemesterId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Internships");
                
            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Theses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // First, add back the old columns as nullable
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "Internships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "Internships",
                type: "int",
                nullable: true);
                
            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId",
                table: "Theses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "Theses",
                type: "int",
                nullable: true);

            // Populate the old columns with data from the periods
            migrationBuilder.Sql(@"
                UPDATE i
                SET i.AcademicYearId = p.AcademicYearId, i.SemesterId = p.SemesterId
                FROM Internships i
                INNER JOIN InternshipPeriods p ON i.InternshipPeriodId = p.Id;

                UPDATE t
                SET t.AcademicYearId = p.AcademicYearId, t.SemesterId = p.SemesterId
                FROM Theses t
                INNER JOIN ThesisPeriods p ON t.ThesisPeriodId = p.Id;
            ");
            
            // Now make them non-nullable
            migrationBuilder.AlterColumn<int>(name: "AcademicYearId", table: "Internships", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "SemesterId", table: "Internships", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "AcademicYearId", table: "Theses", nullable: false);
            migrationBuilder.AlterColumn<int>(name: "SemesterId", table: "Theses", nullable: false);


            // Drop the new foreign keys and columns
            migrationBuilder.DropForeignKey(
                name: "FK_Internships_InternshipPeriods_InternshipPeriodId",
                table: "Internships");

            migrationBuilder.DropForeignKey(
                name: "FK_Theses_ThesisPeriods_ThesisPeriodId",
                table: "Theses");

            migrationBuilder.DropTable(
                name: "InternshipPeriods");

            migrationBuilder.DropTable(
                name: "ThesisPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Internships_InternshipPeriodId",
                table: "Internships");
            
            migrationBuilder.DropIndex(
                name: "IX_Theses_ThesisPeriodId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "InternshipPeriodId",
                table: "Internships");
                
            migrationBuilder.DropColumn(
                name: "ThesisPeriodId",
                table: "Theses");

            // Recreate old indexes and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_Internships_AcademicYearId",
                table: "Internships",
                column: "AcademicYearId");
            
            migrationBuilder.CreateIndex(
                name: "IX_Internships_SemesterId",
                table: "Internships",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Theses_AcademicYearId",
                table: "Theses",
                column: "AcademicYearId");
            
            migrationBuilder.CreateIndex(
                name: "IX_Theses_SemesterId",
                table: "Theses",
                column: "SemesterId");

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
    }
}
