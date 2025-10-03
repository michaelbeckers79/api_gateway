using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiGateway.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClusterConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClusterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DestinationAddress = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RouteId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ClusterId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Match = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TokenId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTokens", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClusterConfigs",
                columns: new[] { "Id", "ClusterId", "CreatedAt", "DestinationAddress", "IsActive", "UpdatedAt" },
                values: new object[] { 1, "backend-api", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "http://localhost:5001", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RouteConfigs",
                columns: new[] { "Id", "ClusterId", "CreatedAt", "IsActive", "Match", "Order", "RouteId", "UpdatedAt" },
                values: new object[] { 1, "backend-api", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "/api/{**catch-all}", 1, "api-route", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterConfigs_ClusterId",
                table: "ClusterConfigs",
                column: "ClusterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteConfigs_RouteId",
                table: "RouteConfigs",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTokens_TokenId",
                table: "SessionTokens",
                column: "TokenId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterConfigs");

            migrationBuilder.DropTable(
                name: "RouteConfigs");

            migrationBuilder.DropTable(
                name: "SessionTokens");
        }
    }
}
