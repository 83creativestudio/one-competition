using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCompetitions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailureCount",
                table: "WebhookEndpoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecretCiphertext",
                table: "WebhookEndpoints",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ResponseBodyPreview",
                table: "WebhookDeliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ExportJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                table: "ExportJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DrawCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrawId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Sha256Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrawCertificates_Draws_DrawId",
                        column: x => x.DrawId,
                        principalTable: "Draws",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrawCertificates_DrawId",
                table: "DrawCertificates",
                column: "DrawId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_Status_AvailableAt",
                table: "NotificationMessages",
                columns: new[] { "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_TenantId_CreatedAt",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_TenantId_DeduplicationKey",
                table: "NotificationMessages",
                columns: new[] { "TenantId", "DeduplicationKey" });

            // Legacy endpoints have no recoverable signing secret. Disable them until an owner recreates them.
            migrationBuilder.Sql("UPDATE \"WebhookEndpoints\" SET \"IsActive\" = FALSE WHERE \"SecretCiphertext\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrawCertificates");

            migrationBuilder.DropTable(
                name: "NotificationMessages");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailureCount",
                table: "WebhookEndpoints");

            migrationBuilder.DropColumn(
                name: "SecretCiphertext",
                table: "WebhookEndpoints");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "ResponseBodyPreview",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ExportJobs");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "ExportJobs");
        }
    }
}
