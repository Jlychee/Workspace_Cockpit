using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActionNoteSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "workspace_notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "workspace_actions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE workspace_notes
                SET sort_order = (
                    SELECT COUNT(*)
                    FROM workspace_notes AS other
                    WHERE other.workspace_id = workspace_notes.workspace_id
                      AND (
                          other.updated_at_utc > workspace_notes.updated_at_utc
                          OR (other.updated_at_utc = workspace_notes.updated_at_utc AND other.id < workspace_notes.id)
                      )
                );
                """);

            migrationBuilder.Sql("""
                UPDATE workspace_actions
                SET sort_order = (
                    SELECT COUNT(*)
                    FROM workspace_actions AS other
                    WHERE other.workspace_id = workspace_actions.workspace_id
                      AND (
                          other.name < workspace_actions.name
                          OR (other.name = workspace_actions.name AND other.id < workspace_actions.id)
                      )
                );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_notes_workspace_id_sort_order",
                table: "workspace_notes",
                columns: new[] { "workspace_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_workspace_actions_workspace_id_sort_order",
                table: "workspace_actions",
                columns: new[] { "workspace_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_workspace_notes_workspace_id_sort_order",
                table: "workspace_notes");

            migrationBuilder.DropIndex(
                name: "ix_workspace_actions_workspace_id_sort_order",
                table: "workspace_actions");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "workspace_notes");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "workspace_actions");
        }
    }
}
