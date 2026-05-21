using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLVisualExplorer.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comparisons",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    query_a = table.Column<string>(type: "TEXT", nullable: false),
                    query_b = table.Column<string>(type: "TEXT", nullable: false),
                    result_json = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comparisons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    db_type = table.Column<string>(type: "TEXT", nullable: false),
                    host = table.Column<string>(type: "TEXT", nullable: true),
                    port = table.Column<int>(type: "INTEGER", nullable: true),
                    database = table.Column<string>(type: "TEXT", nullable: false),
                    username = table.Column<string>(type: "TEXT", nullable: true),
                    use_ssl = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_used = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snippets",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    sql_text = table.Column<string>(type: "TEXT", nullable: false),
                    tags = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snippets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "query_history",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    connection_id = table.Column<string>(type: "TEXT", nullable: true),
                    sql_text = table.Column<string>(type: "TEXT", nullable: false),
                    executed_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    duration_ms = table.Column<long>(type: "INTEGER", nullable: true),
                    row_count = table.Column<long>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    explain_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_query_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_query_history_connections_connection_id",
                        column: x => x.connection_id,
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_query_history_connection_id",
                table: "query_history",
                column: "connection_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comparisons");

            migrationBuilder.DropTable(
                name: "query_history");

            migrationBuilder.DropTable(
                name: "snippets");

            migrationBuilder.DropTable(
                name: "connections");
        }
    }
}
