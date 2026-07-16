using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sessions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    Score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTeams_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionTeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAlias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTeamMembers_SessionTeams_SessionTeamId",
                        column: x => x.SessionTeamId,
                        principalTable: "SessionTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionTeams_SessionId_Name",
                table: "SessionTeams",
                columns: new[] { "SessionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTeamMembers_SessionTeamId_UserId",
                table: "SessionTeamMembers",
                columns: new[] { "SessionTeamId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SessionTeamMembers");
            migrationBuilder.DropTable(name: "SessionTeams");
        }
    }
}
