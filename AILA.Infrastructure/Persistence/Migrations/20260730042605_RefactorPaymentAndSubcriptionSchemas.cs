using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaymentAndSubcriptionSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "AiPracticeScenarioLimit",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AiTokenLimit",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationInDays",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpertEvaluationLimit",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TierLevel",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments",
                column: "TransactionCode",
                unique: true,
                filter: "\"TransactionCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AiPracticeScenarioLimit",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AiTokenLimit",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DurationInDays",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpertEvaluationLimit",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TierLevel",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments",
                column: "TransactionCode");
        }
    }
}
