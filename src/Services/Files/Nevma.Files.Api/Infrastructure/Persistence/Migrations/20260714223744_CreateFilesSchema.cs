using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Files.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateFilesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.CreateTable(
                name: "file_assets",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_access_grants",
                schema: "files",
                columns: table => new
                {
                    FileAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_access_grants", x => new { x.FileAssetId, x.UserId });
                    table.ForeignKey(
                        name: "FK_file_access_grants_file_assets_FileAssetId",
                        column: x => x.FileAssetId,
                        principalSchema: "files",
                        principalTable: "file_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_access_grants_UserId",
                schema: "files",
                table: "file_access_grants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_file_assets_OwnerId_CreatedAt",
                schema: "files",
                table: "file_assets",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_file_assets_StorageKey",
                schema: "files",
                table: "file_assets",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_access_grants",
                schema: "files");

            migrationBuilder.DropTable(
                name: "file_assets",
                schema: "files");
        }
    }
}
