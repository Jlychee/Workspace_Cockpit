using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Entities;

namespace Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkspaceItem> Workspaces => Set<WorkspaceItem>();
    public DbSet<WorkspaceNote> WorkspaceNotes => Set<WorkspaceNote>();
    public DbSet<WorkspaceAction> WorkspaceActions => Set<WorkspaceAction>();
    public DbSet<WorkspaceLog> ActionRuns => Set<WorkspaceLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWorkspaces(modelBuilder.Entity<WorkspaceItem>());
        ConfigureWorkspaceNotes(modelBuilder.Entity<WorkspaceNote>());
        ConfigureWorkspaceActions(modelBuilder.Entity<WorkspaceAction>());
        ConfigureActionRuns(modelBuilder.Entity<WorkspaceLog>());
        ConfigureAppSettings(modelBuilder.Entity<AppSetting>());
    }

    private static void ConfigureWorkspaces(EntityTypeBuilder<WorkspaceItem> workspace)
    {
        workspace.ToTable("workspaces");

        workspace.HasKey(x => x.Id);

        workspace.Property(x => x.Id)
            .HasColumnName("id");
        workspace.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();
        workspace.Property(x => x.RootPath)
            .HasColumnName("root_path")
            .IsRequired();
        workspace.Property(x => x.ResumeText)
            .HasColumnName("resume_text")
            .IsRequired();

        workspace.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        workspace.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        workspace.Property(x => x.LastOpenedAtUtc)
            .HasColumnName("last_opened_at_utc");

        workspace.Ignore(x => x.Meta);
        workspace.Ignore(x => x.CreatedText);
        workspace.Ignore(x => x.UpdatedText);
        workspace.Ignore(x => x.LastOpenedText);
        workspace.Ignore(x => x.DetailsText);

        workspace
            .HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        workspace
            .HasMany(x => x.Actions)
            .WithOne()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        workspace.HasIndex(x => x.UpdatedAtUtc).HasDatabaseName("ix_workspaces_updated_at_utc");
        workspace.HasIndex(x => x.LastOpenedAtUtc).HasDatabaseName("ix_workspaces_last_opened_at_utc");
    }

    private static void ConfigureWorkspaceNotes(EntityTypeBuilder<WorkspaceNote> note)
    {
        note.ToTable("workspace_notes");
        note.HasKey(x => x.Id);

        note.Property(x => x.Id).HasColumnName("id");
        note.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        note.Property(x => x.Type).HasColumnName("type").IsRequired();
        note.Property(x => x.Text).HasColumnName("text").IsRequired();
        note.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        note.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        note.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        note.Ignore(x => x.Header);
        note.Ignore(x => x.UpdatedText);

        note.HasIndex(x => x.WorkspaceId).HasDatabaseName("ix_workspace_notes_workspace_id");
        note.HasIndex(x => new { x.WorkspaceId, x.SortOrder }).HasDatabaseName("ix_workspace_notes_workspace_id_sort_order");
        note.HasIndex(x => x.UpdatedAtUtc).HasDatabaseName("ix_workspace_notes_updated_at_utc");
    }

    private static void ConfigureWorkspaceActions(EntityTypeBuilder<WorkspaceAction> action)
    {
        action.ToTable("workspace_actions");
        action.HasKey(x => x.Id);

        action.Property(x => x.Id).HasColumnName("id");
        action.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        action.Property(x => x.Name).HasColumnName("name").IsRequired();
        action.Property(x => x.Target).HasColumnName("target").IsRequired();
        action.Property(x => x.WorkingDirectory).HasColumnName("working_directory").IsRequired();
        action.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        action.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasColumnName("action_type")
            .IsRequired();
        action.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        action.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        action.Property(x => x.LastRunAtUtc).HasColumnName("last_run_at_utc");

        action.Ignore(x => x.LastRunText);

        action
            .HasMany(x => x.ActionRuns)
            .WithOne()
            .HasForeignKey(x => x.WorkspaceActionId)
            .OnDelete(DeleteBehavior.Cascade);

        action.HasIndex(x => x.WorkspaceId).HasDatabaseName("ix_workspace_actions_workspace_id");
        action.HasIndex(x => new { x.WorkspaceId, x.SortOrder }).HasDatabaseName("ix_workspace_actions_workspace_id_sort_order");
        action.HasIndex(x => x.UpdatedAtUtc).HasDatabaseName("ix_workspace_actions_updated_at_utc");
    }

    private static void ConfigureActionRuns(EntityTypeBuilder<WorkspaceLog> run)
    {
        run.ToTable("action_runs");
        run.HasKey(x => x.Id);

        run.Property(x => x.Id).HasColumnName("id");
        run.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        run.Property(x => x.WorkspaceActionId).HasColumnName("workspace_action_id");
        run.Property(x => x.ActionNameSnapshot).HasColumnName("action_name_snapshot").IsRequired();
        run.Property(x => x.TargetSnapshot).HasColumnName("target_snapshot").IsRequired();
        run.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc").IsRequired();
        run.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");
        run.Property(x => x.Status).HasColumnName("status").IsRequired();
        run.Property(x => x.ExitCode).HasColumnName("exit_code");
        run.Property(x => x.DurationMs).HasColumnName("duration_ms");
        run.Property(x => x.OutputPreview).HasColumnName("output_preview").IsRequired();
        run.Property(x => x.ErrorMessage).HasColumnName("error_message").IsRequired();

        run.Ignore(x => x.StartedText);
        run.Ignore(x => x.DurationText);
        run.Ignore(x => x.ExitCodeText);

        run.HasIndex(x => x.WorkspaceId).HasDatabaseName("ix_action_runs_workspace_id");
        run.HasIndex(x => x.WorkspaceActionId).HasDatabaseName("ix_action_runs_workspace_action_id");
        run.HasIndex(x => x.StartedAtUtc).HasDatabaseName("ix_action_runs_started_at_utc");
    }

    private static void ConfigureAppSettings(EntityTypeBuilder<AppSetting> setting)
    {
        setting.ToTable("app_settings");
        setting.HasKey(x => x.Key);

        setting.Property(x => x.Key).HasColumnName("key");
        setting.Property(x => x.Value).HasColumnName("value").IsRequired();
    }
}
