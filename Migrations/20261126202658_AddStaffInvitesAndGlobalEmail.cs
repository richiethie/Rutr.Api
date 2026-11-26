using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rutr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffInvitesAndGlobalEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffUsers_OrganizationId_Email",
                table: "StaffUsers");

            migrationBuilder.CreateTable(
                name: "StaffInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    InviteToken = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffInvites_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffUsers_Email",
                table: "StaffUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffUsers_OrganizationId",
                table: "StaffUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvites_InviteToken",
                table: "StaffInvites",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvites_OrganizationId_Email",
                table: "StaffInvites",
                columns: new[] { "OrganizationId", "Email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffInvites");

            migrationBuilder.DropIndex(
                name: "IX_StaffUsers_Email",
                table: "StaffUsers");

            migrationBuilder.DropIndex(
                name: "IX_StaffUsers_OrganizationId",
                table: "StaffUsers");

            migrationBuilder.CreateIndex(
                name: "IX_StaffUsers_OrganizationId_Email",
                table: "StaffUsers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);
        }
    }
}
