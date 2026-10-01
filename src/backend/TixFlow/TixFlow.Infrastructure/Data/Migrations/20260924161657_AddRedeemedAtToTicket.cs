using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRedeemedAtToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RedeemedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedeemedAt",
                table: "Tickets");
        }
    }
}
