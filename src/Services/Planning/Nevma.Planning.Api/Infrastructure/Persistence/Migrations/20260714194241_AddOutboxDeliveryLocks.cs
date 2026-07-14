using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Planning.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeliveryLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAt_NextAttemptAt",
                schema: "planning",
                table: "outbox_messages");

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                schema: "planning",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                schema: "planning",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_NextAttemptAt_LockedUntil",
                schema: "planning",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "NextAttemptAt", "LockedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAt_NextAttemptAt_LockedUntil",
                schema: "planning",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LockId",
                schema: "planning",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                schema: "planning",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_NextAttemptAt",
                schema: "planning",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "NextAttemptAt" });
        }
    }
}
