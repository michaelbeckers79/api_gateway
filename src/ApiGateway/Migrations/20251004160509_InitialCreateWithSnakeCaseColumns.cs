using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiGateway.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithSnakeCaseColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCredentials",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    client_secret_hash = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCredentials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ClusterConfigs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cluster_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    destination_address = table.Column<string>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterConfigs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RouteConfigs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    route_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    cluster_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    match = table.Column<string>(type: "TEXT", nullable: false),
                    order = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    security_policy = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteConfigs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RoutePolicies",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    route_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    security_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    token_endpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    client_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    client_secret = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    scope = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    token_expiration_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePolicies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    token_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    access_token = table.Column<string>(type: "TEXT", nullable: false),
                    refresh_token = table.Column<string>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_accessed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    is_revoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    ip_address = table.Column<string>(type: "TEXT", nullable: true),
                    user_agent = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_SessionTokens_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpstreamTokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    route_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    session_id = table.Column<int>(type: "INTEGER", nullable: true),
                    access_token = table.Column<string>(type: "TEXT", nullable: false),
                    refresh_token = table.Column<string>(type: "TEXT", nullable: true),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_refreshed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpstreamTokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_UpstreamTokens_SessionTokens_session_id",
                        column: x => x.session_id,
                        principalTable: "SessionTokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ClusterConfigs",
                columns: new[] { "id", "cluster_id", "created_at", "destination_address", "is_active", "updated_at" },
                values: new object[] { 1, "backend-api", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "http://localhost:5001", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RouteConfigs",
                columns: new[] { "id", "cluster_id", "created_at", "is_active", "match", "order", "route_id", "security_policy", "updated_at" },
                values: new object[] { 1, "backend-api", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "/api/{**catch-all}", 1, "api-route", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_ClientCredentials_client_id",
                table: "ClientCredentials",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClusterConfigs_cluster_id",
                table: "ClusterConfigs",
                column: "cluster_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteConfigs_route_id",
                table: "RouteConfigs",
                column: "route_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutePolicies_route_id",
                table: "RoutePolicies",
                column: "route_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTokens_token_id",
                table: "SessionTokens",
                column: "token_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTokens_user_id",
                table: "SessionTokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UpstreamTokens_route_id_session_id",
                table: "UpstreamTokens",
                columns: new[] { "route_id", "session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_UpstreamTokens_session_id",
                table: "UpstreamTokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_username",
                table: "Users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientCredentials");

            migrationBuilder.DropTable(
                name: "ClusterConfigs");

            migrationBuilder.DropTable(
                name: "RouteConfigs");

            migrationBuilder.DropTable(
                name: "RoutePolicies");

            migrationBuilder.DropTable(
                name: "UpstreamTokens");

            migrationBuilder.DropTable(
                name: "SessionTokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
