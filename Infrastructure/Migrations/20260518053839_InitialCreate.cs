using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    root_path = table.Column<string>(type: "TEXT", nullable: false),
                    resume_text = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_opened_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    workspace_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    target = table.Column<string>(type: "TEXT", nullable: false),
                    working_directory = table.Column<string>(type: "TEXT", nullable: false),
                    action_type = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_run_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_actions_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    workspace_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_workspace_notes_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "action_runs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    workspace_id = table.Column<int>(type: "INTEGER", nullable: false),
                    workspace_action_id = table.Column<int>(type: "INTEGER", nullable: false),
                    action_name_snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    target_snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    finished_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    exit_code = table.Column<int>(type: "INTEGER", nullable: true),
                    duration_ms = table.Column<long>(type: "INTEGER", nullable: true),
                    output_preview = table.Column<string>(type: "TEXT", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_action_runs_workspace_actions_workspace_action_id",
                        column: x => x.workspace_action_id,
                        principalTable: "workspace_actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_action_runs_started_at_utc",
                table: "action_runs",
                column: "started_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_action_runs_workspace_action_id",
                table: "action_runs",
                column: "workspace_action_id");

            migrationBuilder.CreateIndex(
                name: "ix_action_runs_workspace_id",
                table: "action_runs",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_actions_updated_at_utc",
                table: "workspace_actions",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_actions_workspace_id",
                table: "workspace_actions",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_notes_updated_at_utc",
                table: "workspace_notes",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_notes_workspace_id",
                table: "workspace_notes",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_last_opened_at_utc",
                table: "workspaces",
                column: "last_opened_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_updated_at_utc",
                table: "workspaces",
                column: "updated_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "action_runs");

            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "workspace_notes");

            migrationBuilder.DropTable(
                name: "workspace_actions");

            migrationBuilder.DropTable(
                name: "workspaces");
        }
    }
}
