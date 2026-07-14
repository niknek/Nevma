using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nevma.Planning.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationCounterProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProposedDuration",
                schema: "planning",
                table: "meeting_invitations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedLocation",
                schema: "planning",
                table: "meeting_invitations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedStartsAt",
                schema: "planning",
                table: "meeting_invitations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedDuration",
                schema: "planning",
                table: "meeting_invitations");

            migrationBuilder.DropColumn(
                name: "ProposedLocation",
                schema: "planning",
                table: "meeting_invitations");

            migrationBuilder.DropColumn(
                name: "ProposedStartsAt",
                schema: "planning",
                table: "meeting_invitations");
        }
    }
}
