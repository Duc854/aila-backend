using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserViolationRecordSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptId",
                table: "UserViolationRecords");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "UserViolationRecords");

            migrationBuilder.AddColumn<string>(
                name: "ViolatingPrompt",
                table: "UserViolationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViolatingPrompt",
                table: "UserViolationRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "AttemptId",
                table: "UserViolationRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "UserViolationRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Medium");
        }
    }
}
