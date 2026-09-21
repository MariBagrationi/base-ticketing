using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTierIdToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TicketTierId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketTierId",
                table: "Tickets",
                column: "TicketTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TicketTiers_TicketTierId",
                table: "Tickets",
                column: "TicketTierId",
                principalTable: "TicketTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TicketTiers_TicketTierId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_TicketTierId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TicketTierId",
                table: "Tickets");
        }
    }
}
