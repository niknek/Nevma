using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Planning.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteTaskWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tasks_Status_ReminderAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "planning",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recurrence",
                schema: "planning",
                table: "tasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecurrenceEndsAt",
                schema: "planning",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                schema: "planning",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReminderDispatchedAt",
                schema: "planning",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "planning",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tasks_Status_ReminderAt_ReminderDispatchedAt",
                schema: "planning",
                table: "tasks",
                columns: new[] { "Status", "ReminderAt", "ReminderDispatchedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tasks_Status_ReminderAt_ReminderDispatchedAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "Recurrence",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndsAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "ReminderDispatchedAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "planning",
                table: "tasks");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_Status_ReminderAt",
                schema: "planning",
                table: "tasks",
                columns: new[] { "Status", "ReminderAt" });
        }
    }
}
