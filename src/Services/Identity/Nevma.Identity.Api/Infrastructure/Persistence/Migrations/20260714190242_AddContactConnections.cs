using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Identity.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_connections",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddresseeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PairFirstUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PairSecondUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contact_connections_AspNetUsers_AddresseeId",
                        column: x => x.AddresseeId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contact_connections_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contact_connections_AddresseeId_Status",
                schema: "identity",
                table: "contact_connections",
                columns: new[] { "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_contact_connections_PairFirstUserId_PairSecondUserId",
                schema: "identity",
                table: "contact_connections",
                columns: new[] { "PairFirstUserId", "PairSecondUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contact_connections_RequesterId_Status",
                schema: "identity",
                table: "contact_connections",
                columns: new[] { "RequesterId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_connections",
                schema: "identity");
        }
    }
}
