using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rutr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DraftId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StaffUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId_JobId_CreatedAt",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "JobId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
