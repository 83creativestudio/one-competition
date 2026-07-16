using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCompetitions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteProductionReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvitationExpiresAt",
                table: "TenantUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationTokenHash",
                table: "TenantUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "TenantSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCustomerId",
                table: "TenantSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedValue",
                table: "EntryAnswers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedCountryCodesJson",
                table: "Competitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MinimumAge",
                table: "Competitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKeyHash",
                table: "CompetitionEntries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntryVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RequestType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResultStorageKey = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resellers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resellers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionEntries_CompetitionId_IdempotencyKeyHash",
                table: "CompetitionEntries",
                columns: new[] { "CompetitionId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingWebhookEvents_Provider_ExternalEventId",
                table: "BillingWebhookEvents",
                columns: new[] { "Provider", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntryVerifications_EntryId_Channel",
                table: "EntryVerifications",
                columns: new[] { "EntryId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_TenantId_Reference",
                table: "PrivacyRequests",
                columns: new[] { "TenantId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resellers_Slug",
                table: "Resellers",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingWebhookEvents");

            migrationBuilder.DropTable(
                name: "EntryVerifications");

            migrationBuilder.DropTable(
                name: "PrivacyRequests");

            migrationBuilder.DropTable(
                name: "Resellers");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionEntries_CompetitionId_IdempotencyKeyHash",
                table: "CompetitionEntries");

            migrationBuilder.DropColumn(
                name: "InvitationExpiresAt",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "InvitationTokenHash",
                table: "TenantUsers");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalCustomerId",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "EncryptedValue",
                table: "EntryAnswers");

            migrationBuilder.DropColumn(
                name: "AllowedCountryCodesJson",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "MinimumAge",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKeyHash",
                table: "CompetitionEntries");
        }
    }
}
