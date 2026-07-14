using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "messaging",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EditedAt",
                schema: "messaging",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToMessageId",
                schema: "messaging",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "messaging",
                table: "messages",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "message_reactions",
                schema: "messaging",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_reactions", x => new { x.MessageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_message_reactions_messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "messaging",
                        principalTable: "messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_receipts",
                schema: "messaging",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_receipts", x => new { x.MessageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_message_receipts_messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "messaging",
                        principalTable: "messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_messages_ReplyToMessageId",
                schema: "messaging",
                table: "messages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_message_receipts_UserId_ReadAt",
                schema: "messaging",
                table: "message_receipts",
                columns: new[] { "UserId", "ReadAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_messages_messages_ReplyToMessageId",
                schema: "messaging",
                table: "messages",
                column: "ReplyToMessageId",
                principalSchema: "messaging",
                principalTable: "messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_messages_ReplyToMessageId",
                schema: "messaging",
                table: "messages");

            migrationBuilder.DropTable(
                name: "message_reactions",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "message_receipts",
                schema: "messaging");

            migrationBuilder.DropIndex(
                name: "IX_messages_ReplyToMessageId",
                schema: "messaging",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "messaging",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                schema: "messaging",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                schema: "messaging",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "messaging",
                table: "messages");
        }
    }
}
