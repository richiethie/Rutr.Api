using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rutr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeownerConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeownerConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SmsOptIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailOptIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsStopped = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeownerConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeownerConsents_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeownerConsents_JobId",
                table: "HomeownerConsents",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeownerConsents_OrganizationId_JobId",
                table: "HomeownerConsents",
                columns: new[] { "OrganizationId", "JobId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeownerConsents");
        }
    }
}
