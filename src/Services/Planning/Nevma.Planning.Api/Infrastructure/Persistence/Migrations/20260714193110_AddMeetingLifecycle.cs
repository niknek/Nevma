using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Planning.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_calendar_events_StartsAt",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                schema: "planning",
                table: "calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "planning",
                table: "calendar_events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "planning",
                table: "calendar_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE planning.calendar_events
                SET "Status" = 'Confirmed', "UpdatedAt" = "StartsAt";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "planning",
                table: "calendar_events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "planning",
                table: "calendar_events",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "planning",
                table: "calendar_events",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_Status_StartsAt_EndsAt",
                schema: "planning",
                table: "calendar_events",
                columns: new[] { "Status", "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_calendar_events_Status_StartsAt_EndsAt",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "planning",
                table: "calendar_events");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_StartsAt",
                schema: "planning",
                table: "calendar_events",
                column: "StartsAt");
        }
    }
}
