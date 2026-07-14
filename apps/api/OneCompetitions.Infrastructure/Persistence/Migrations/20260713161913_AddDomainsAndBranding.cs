using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneCompetitions.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainsAndBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrandProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    DarkLogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaviconAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeadingFont = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BodyFont = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ButtonStyle = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BorderRadius = table.Column<int>(type: "integer", nullable: false),
                    EmailHeaderAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    SocialShareAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    FooterText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SupportEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    SupportPhone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ShowPoweredBy = table.Column<bool>(type: "boolean", nullable: false),
                    CustomCss = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandProfiles_TenantId_Name",
                table: "BrandProfiles",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandProfiles");
        }
    }
}
