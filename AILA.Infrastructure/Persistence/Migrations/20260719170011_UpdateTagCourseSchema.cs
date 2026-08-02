using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AILA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTagCourseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagPublishRequest_Tags_TagId",
                table: "TagPublishRequest");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Modules");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "TagPublishRequest",
                newName: "ReviewComment");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TagPublishRequest",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "RequestNote",
                table: "TagPublishRequest",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedById",
                table: "TagPublishRequest",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TagPublishRequest_RequestedById",
                table: "TagPublishRequest",
                column: "RequestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_TagPublishRequest_Tags_TagId",
                table: "TagPublishRequest",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TagPublishRequest_Users_RequestedById",
                table: "TagPublishRequest",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagPublishRequest_Tags_TagId",
                table: "TagPublishRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_TagPublishRequest_Users_RequestedById",
                table: "TagPublishRequest");

            migrationBuilder.DropIndex(
                name: "IX_TagPublishRequest_RequestedById",
                table: "TagPublishRequest");

            migrationBuilder.DropColumn(
                name: "RequestNote",
                table: "TagPublishRequest");

            migrationBuilder.DropColumn(
                name: "RequestedById",
                table: "TagPublishRequest");

            migrationBuilder.RenameColumn(
                name: "ReviewComment",
                table: "TagPublishRequest",
                newName: "Note");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TagPublishRequest",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_TagPublishRequest_Tags_TagId",
                table: "TagPublishRequest",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
