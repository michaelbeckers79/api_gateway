using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiGateway.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToSnakeCaseAndAddUpstreamSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionTokens_Users_UserId",
                table: "SessionTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SessionTokens",
                table: "SessionTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RouteConfigs",
                table: "RouteConfigs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClusterConfigs",
                table: "ClusterConfigs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClientCredentials",
                table: "ClientCredentials");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "SessionTokens",
                newName: "session_tokens");

            migrationBuilder.RenameTable(
                name: "RouteConfigs",
                newName: "route_configs");

            migrationBuilder.RenameTable(
                name: "ClusterConfigs",
                newName: "cluster_configs");

            migrationBuilder.RenameTable(
                name: "ClientCredentials",
                newName: "client_credentials");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "users",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "IX_users_username");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "session_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "session_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "session_tokens",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "session_tokens",
                newName: "token_id");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "session_tokens",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "LastAccessedAt",
                table: "session_tokens",
                newName: "last_accessed_at");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                table: "session_tokens",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "session_tokens",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "session_tokens",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "session_tokens",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "session_tokens",
                newName: "access_token");

            migrationBuilder.RenameIndex(
                name: "IX_SessionTokens_UserId",
                table: "session_tokens",
                newName: "i_x_session_tokens_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_SessionTokens_TokenId",
                table: "session_tokens",
                newName: "IX_session_tokens_token_id");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "route_configs",
                newName: "order");

            migrationBuilder.RenameColumn(
                name: "Match",
                table: "route_configs",
                newName: "match");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "route_configs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "route_configs",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "route_configs",
                newName: "route_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "route_configs",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "route_configs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ClusterId",
                table: "route_configs",
                newName: "cluster_id");

            migrationBuilder.RenameIndex(
                name: "IX_RouteConfigs_RouteId",
                table: "route_configs",
                newName: "IX_route_configs_route_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cluster_configs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "cluster_configs",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "cluster_configs",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DestinationAddress",
                table: "cluster_configs",
                newName: "destination_address");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "cluster_configs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ClusterId",
                table: "cluster_configs",
                newName: "cluster_id");

            migrationBuilder.RenameIndex(
                name: "IX_ClusterConfigs_ClusterId",
                table: "cluster_configs",
                newName: "IX_cluster_configs_cluster_id");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "client_credentials",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "client_credentials",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                table: "client_credentials",
                newName: "last_used_at");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "client_credentials",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "client_credentials",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ClientSecretHash",
                table: "client_credentials",
                newName: "client_secret_hash");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "client_credentials",
                newName: "client_id");

            migrationBuilder.RenameIndex(
                name: "IX_ClientCredentials_ClientId",
                table: "client_credentials",
                newName: "IX_client_credentials_client_id");

            migrationBuilder.AddColumn<string>(
                name: "security_policy",
                table: "route_configs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "p_k_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_session_tokens",
                table: "session_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_route_configs",
                table: "route_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_cluster_configs",
                table: "cluster_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "p_k_client_credentials",
                table: "client_credentials",
                column: "id");

            migrationBuilder.CreateTable(
                name: "route_policies",
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
                    table.PrimaryKey("p_k_route_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upstream_tokens",
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
                    table.PrimaryKey("p_k_upstream_tokens", x => x.id);
                    table.ForeignKey(
                        name: "f_k_upstream_tokens_session_tokens_session_id",
                        column: x => x.session_id,
                        principalTable: "session_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "route_configs",
                keyColumn: "id",
                keyValue: 1,
                column: "security_policy",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_route_policies_route_id",
                table: "route_policies",
                column: "route_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_upstream_tokens_session_id",
                table: "upstream_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_upstream_tokens_route_id_session_id",
                table: "upstream_tokens",
                columns: new[] { "route_id", "session_id" });

            migrationBuilder.AddForeignKey(
                name: "f_k_session_tokens__users_user_id",
                table: "session_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_session_tokens__users_user_id",
                table: "session_tokens");

            migrationBuilder.DropTable(
                name: "route_policies");

            migrationBuilder.DropTable(
                name: "upstream_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_session_tokens",
                table: "session_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_route_configs",
                table: "route_configs");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_cluster_configs",
                table: "cluster_configs");

            migrationBuilder.DropPrimaryKey(
                name: "p_k_client_credentials",
                table: "client_credentials");

            migrationBuilder.DropColumn(
                name: "security_policy",
                table: "route_configs");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "session_tokens",
                newName: "SessionTokens");

            migrationBuilder.RenameTable(
                name: "route_configs",
                newName: "RouteConfigs");

            migrationBuilder.RenameTable(
                name: "cluster_configs",
                newName: "ClusterConfigs");

            migrationBuilder.RenameTable(
                name: "client_credentials",
                newName: "ClientCredentials");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "Users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "Users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "Users",
                newName: "IsEnabled");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_users_username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SessionTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "SessionTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                table: "SessionTokens",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "token_id",
                table: "SessionTokens",
                newName: "TokenId");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "SessionTokens",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "last_accessed_at",
                table: "SessionTokens",
                newName: "LastAccessedAt");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                table: "SessionTokens",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "SessionTokens",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "SessionTokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SessionTokens",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "access_token",
                table: "SessionTokens",
                newName: "AccessToken");

            migrationBuilder.RenameIndex(
                name: "IX_session_tokens_token_id",
                table: "SessionTokens",
                newName: "IX_SessionTokens_TokenId");

            migrationBuilder.RenameIndex(
                name: "i_x_session_tokens_user_id",
                table: "SessionTokens",
                newName: "IX_SessionTokens_UserId");

            migrationBuilder.RenameColumn(
                name: "order",
                table: "RouteConfigs",
                newName: "Order");

            migrationBuilder.RenameColumn(
                name: "match",
                table: "RouteConfigs",
                newName: "Match");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RouteConfigs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "RouteConfigs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "route_id",
                table: "RouteConfigs",
                newName: "RouteId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "RouteConfigs",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "RouteConfigs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cluster_id",
                table: "RouteConfigs",
                newName: "ClusterId");

            migrationBuilder.RenameIndex(
                name: "IX_route_configs_route_id",
                table: "RouteConfigs",
                newName: "IX_RouteConfigs_RouteId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ClusterConfigs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ClusterConfigs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "ClusterConfigs",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "destination_address",
                table: "ClusterConfigs",
                newName: "DestinationAddress");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ClusterConfigs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cluster_id",
                table: "ClusterConfigs",
                newName: "ClusterId");

            migrationBuilder.RenameIndex(
                name: "IX_cluster_configs_cluster_id",
                table: "ClusterConfigs",
                newName: "IX_ClusterConfigs_ClusterId");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "ClientCredentials",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ClientCredentials",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_used_at",
                table: "ClientCredentials",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "ClientCredentials",
                newName: "IsEnabled");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ClientCredentials",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "client_secret_hash",
                table: "ClientCredentials",
                newName: "ClientSecretHash");

            migrationBuilder.RenameColumn(
                name: "client_id",
                table: "ClientCredentials",
                newName: "ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_client_credentials_client_id",
                table: "ClientCredentials",
                newName: "IX_ClientCredentials_ClientId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SessionTokens",
                table: "SessionTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RouteConfigs",
                table: "RouteConfigs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClusterConfigs",
                table: "ClusterConfigs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClientCredentials",
                table: "ClientCredentials",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionTokens_Users_UserId",
                table: "SessionTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
