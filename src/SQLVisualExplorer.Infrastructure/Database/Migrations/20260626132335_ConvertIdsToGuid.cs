using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLVisualExplorer.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConvertIdsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "database_type",
                table: "query_history",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "database_type",
                table: "query_history");
        }
    }
}
