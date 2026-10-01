using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCompetitions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantSocialAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParticipantIdentities_Provider_ProviderSubjectHash",
                table: "ParticipantIdentities");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderEmail",
                table: "ParticipantIdentities",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessTokenCiphertext",
                table: "ParticipantIdentities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "ParticipantIdentities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GrantedScopes",
                table: "ParticipantIdentities",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSubjectCiphertext",
                table: "ParticipantIdentities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderUserName",
                table: "ParticipantIdentities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenCiphertext",
                table: "ParticipantIdentities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ParticipantIdentities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE "ParticipantIdentities"
                SET "TenantId" = (
                    SELECT "TenantId" FROM "Participants"
                    WHERE "Participants"."Id" = "ParticipantIdentities"."ParticipantId"
                );
                """);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TokenExpiresAt",
                table: "ParticipantIdentities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedParticipantAuthProvidersJson",
                table: "Competitions",
                type: "text",
                nullable: false,
                defaultValue: "[\"Email\"]");

            migrationBuilder.AddColumn<string>(
                name: "EntryMethod",
                table: "CompetitionEntries",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Email");

            migrationBuilder.AddColumn<Guid>(
                name: "ParticipantIdentityId",
                table: "CompetitionEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PkceChallenge = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PkceVerifierCiphertext = table.Column<string>(type: "text", nullable: false),
                    NonceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NonceCiphertext = table.Column<string>(type: "text", nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthTransactions_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthTransactions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionSocialActionRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TargetReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionSocialActionRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitionSocialActionRequirements_Competitions_Competitio~",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompetitionSocialActionRequirements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantSessions_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSessions_ParticipantIdentities_ParticipantIdenti~",
                        column: x => x.ParticipantIdentityId,
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSessions_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialAuthCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAuthCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialAuthCompletions_AuthTransactions_AuthTransactionId",
                        column: x => x.AuthTransactionId,
                        principalTable: "AuthTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAuthCompletions_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAuthCompletions_ParticipantIdentities_ParticipantIden~",
                        column: x => x.ParticipantIdentityId,
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAuthCompletions_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialAuthCompletions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantSocialActionVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantIdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EvidenceJson = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantSocialActionVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantSocialActionVerifications_CompetitionSocialActio~",
                        column: x => x.RequirementId,
                        principalTable: "CompetitionSocialActionRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSocialActionVerifications_Competitions_Competiti~",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSocialActionVerifications_ParticipantIdentities_~",
                        column: x => x.ParticipantIdentityId,
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSocialActionVerifications_Participants_Participa~",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantSocialActionVerifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantIdentities_TenantId_Provider_ProviderSubjectHash",
                table: "ParticipantIdentities",
                columns: new[] { "TenantId", "Provider", "ProviderSubjectHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthTransactions_CompetitionId",
                table: "AuthTransactions",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthTransactions_ExpiresAt",
                table: "AuthTransactions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthTransactions_StateHash",
                table: "AuthTransactions",
                column: "StateHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthTransactions_TenantId",
                table: "AuthTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionSocialActionRequirements_CompetitionId_Provider_~",
                table: "CompetitionSocialActionRequirements",
                columns: new[] { "CompetitionId", "Provider", "ActionType", "TargetReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionSocialActionRequirements_TenantId",
                table: "CompetitionSocialActionRequirements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_CompetitionId",
                table: "ParticipantSessions",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_ExpiresAt",
                table: "ParticipantSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_ParticipantId",
                table: "ParticipantSessions",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_ParticipantIdentityId",
                table: "ParticipantSessions",
                column: "ParticipantIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_TenantId",
                table: "ParticipantSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSessions_TokenHash",
                table: "ParticipantSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSocialActionVerifications_CompetitionId",
                table: "ParticipantSocialActionVerifications",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSocialActionVerifications_ParticipantId",
                table: "ParticipantSocialActionVerifications",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSocialActionVerifications_ParticipantIdentityId",
                table: "ParticipantSocialActionVerifications",
                column: "ParticipantIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSocialActionVerifications_RequirementId_Particip~",
                table: "ParticipantSocialActionVerifications",
                columns: new[] { "RequirementId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantSocialActionVerifications_TenantId",
                table: "ParticipantSocialActionVerifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_AuthTransactionId",
                table: "SocialAuthCompletions",
                column: "AuthTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_CodeHash",
                table: "SocialAuthCompletions",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_CompetitionId",
                table: "SocialAuthCompletions",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_ExpiresAt",
                table: "SocialAuthCompletions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_ParticipantId",
                table: "SocialAuthCompletions",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_ParticipantIdentityId",
                table: "SocialAuthCompletions",
                column: "ParticipantIdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAuthCompletions_TenantId",
                table: "SocialAuthCompletions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantIdentities_Tenants_TenantId",
                table: "ParticipantIdentities",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantIdentities_Tenants_TenantId",
                table: "ParticipantIdentities");

            migrationBuilder.DropTable(
                name: "ParticipantSessions");

            migrationBuilder.DropTable(
                name: "ParticipantSocialActionVerifications");

            migrationBuilder.DropTable(
                name: "SocialAuthCompletions");

            migrationBuilder.DropTable(
                name: "CompetitionSocialActionRequirements");

            migrationBuilder.DropTable(
                name: "AuthTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ParticipantIdentities_TenantId_Provider_ProviderSubjectHash",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "AccessTokenCiphertext",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "GrantedScopes",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "ProviderSubjectCiphertext",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "ProviderUserName",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "RefreshTokenCiphertext",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "ParticipantIdentities");

            migrationBuilder.DropColumn(
                name: "AllowedParticipantAuthProvidersJson",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "EntryMethod",
                table: "CompetitionEntries");

            migrationBuilder.DropColumn(
                name: "ParticipantIdentityId",
                table: "CompetitionEntries");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderEmail",
                table: "ParticipantIdentities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantIdentities_Provider_ProviderSubjectHash",
                table: "ParticipantIdentities",
                columns: new[] { "Provider", "ProviderSubjectHash" },
                unique: true);
        }
    }
}
