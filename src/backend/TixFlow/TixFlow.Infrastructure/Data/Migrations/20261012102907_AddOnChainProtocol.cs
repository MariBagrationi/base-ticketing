using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TixFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOnChainProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketTiers_EventId",
                table: "TicketTiers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FaucetFundedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPerWallet",
                table: "TicketTiers",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "OnChainTierId",
                table: "TicketTiers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BadgeClaimedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckpointedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNonce",
                table: "Orders",
                type: "character varying(66)",
                maxLength: 66,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentSignature",
                table: "Orders",
                type: "character varying(132)",
                maxLength: 132,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentValidAfter",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentValidBefore",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ChainHeadBlock",
                table: "IndexerCheckpoints",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRpcSuccessAt",
                table: "IndexerCheckpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelRequestedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelTxHash",
                table: "Events",
                type: "character varying(66)",
                maxLength: 66,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeploySubmittedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeployTxHash",
                table: "Events",
                type: "character varying(66)",
                maxLength: 66,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscrowReleasedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill rows created before this migration: a default six-hour duration, and 1-based
            // on-chain tier IDs per event in price order.
            migrationBuilder.Sql(
                """UPDATE "Events" SET "EndsAt" = "StartsAt" + interval '6 hours';""");
            migrationBuilder.Sql(
                """
                UPDATE "TicketTiers" t SET "OnChainTierId" = r.rn
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "EventId" ORDER BY "PriceUsdc", "Id") AS rn
                    FROM "TicketTiers"
                ) r
                WHERE t."Id" = r."Id";
                """);

            migrationBuilder.CreateTable(
                name: "ChainActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContractAddress = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: false),
                    TxHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    LogIndex = table.Column<int>(type: "integer", nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    Summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RedemptionCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Root = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    TxHash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedemptionCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedemptionCheckpoints_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTiers_EventId_OnChainTierId",
                table: "TicketTiers",
                columns: new[] { "EventId", "OnChainTierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractAddress",
                table: "Events",
                column: "ContractAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ChainActivities_EventId_BlockNumber",
                table: "ChainActivities",
                columns: new[] { "EventId", "BlockNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ChainActivities_TxHash_LogIndex",
                table: "ChainActivities",
                columns: new[] { "TxHash", "LogIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionCheckpoints_EventId_CreatedAt",
                table: "RedemptionCheckpoints",
                columns: new[] { "EventId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChainActivities");

            migrationBuilder.DropTable(
                name: "RedemptionCheckpoints");

            migrationBuilder.DropIndex(
                name: "IX_TicketTiers_EventId_OnChainTierId",
                table: "TicketTiers");

            migrationBuilder.DropIndex(
                name: "IX_Events_ContractAddress",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "FaucetFundedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxPerWallet",
                table: "TicketTiers");

            migrationBuilder.DropColumn(
                name: "OnChainTierId",
                table: "TicketTiers");

            migrationBuilder.DropColumn(
                name: "BadgeClaimedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CheckpointedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PaymentNonce",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentSignature",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentValidAfter",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentValidBefore",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ChainHeadBlock",
                table: "IndexerCheckpoints");

            migrationBuilder.DropColumn(
                name: "LastRpcSuccessAt",
                table: "IndexerCheckpoints");

            migrationBuilder.DropColumn(
                name: "CancelRequestedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancelTxHash",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DeploySubmittedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DeployTxHash",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EscrowReleasedAt",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTiers_EventId",
                table: "TicketTiers",
                column: "EventId");
        }
    }
}
