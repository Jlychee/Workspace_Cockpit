using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Infrastructure.Repositories;

public class WorkspaceRepository(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<List<WorkspaceItem>> LoadWorkspacesAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.Workspaces
            .Include(x => x.Notes.OrderByDescending(note => note.UpdatedAtUtc))
            .Include(x => x.Actions.OrderBy(action => action.Name))
            .OrderByDescending(x => x.LastOpenedAtUtc ?? x.UpdatedAtUtc)
            .ToListAsync();
    }

    public async Task AddWorkspaceAsync(WorkspaceItem workspace)
    {
        var now = DateTime.UtcNow;
        workspace.CreatedAtUtc = now;
        workspace.UpdatedAtUtc = now;
        workspace.LastOpenedAtUtc = now;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateWorkspaceAsync(WorkspaceItem workspace)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.Workspaces.FindAsync(workspace.Id);

        if (existing is null)
            return;

        existing.Name = workspace.Name;
        existing.RootPath = workspace.RootPath;
        existing.ResumeText = workspace.ResumeText;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        workspace.UpdatedAtUtc = existing.UpdatedAtUtc;
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateWorkspaceLastOpenedAsync(int workspaceId, DateTime lastOpenedAtUtc)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.Workspaces.FindAsync(workspaceId);

        if (existing is null)
            return;

        existing.LastOpenedAtUtc = lastOpenedAtUtc;
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteWorkspaceAsync(WorkspaceItem workspace)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.Workspaces.FindAsync(workspace.Id);

        if (existing is null)
            return;

        dbContext.Workspaces.Remove(existing);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddNoteAsync(int workspaceId, WorkspaceNote note)
    {
        var now = DateTime.UtcNow;
        note.WorkspaceId = workspaceId;
        note.CreatedAtUtc = now;
        note.UpdatedAtUtc = now;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.WorkspaceNotes.Add(note);
        await TouchWorkspaceAsync(dbContext, workspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteNoteAsync(WorkspaceNote note)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.WorkspaceNotes.FindAsync(note.Id);

        if (existing is null)
            return;

        var now = DateTime.UtcNow;
        dbContext.WorkspaceNotes.Remove(existing);
        await TouchWorkspaceAsync(dbContext, existing.WorkspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddActionAsync(int workspaceId, WorkspaceAction action)
    {
        var now = DateTime.UtcNow;
        action.WorkspaceId = workspaceId;
        action.CreatedAtUtc = now;
        action.UpdatedAtUtc = now;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.WorkspaceActions.Add(action);
        await TouchWorkspaceAsync(dbContext, workspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateActionAsync(WorkspaceAction action)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.WorkspaceActions.FindAsync(action.Id);

        if (existing is null)
            return;

        var now = DateTime.UtcNow;
        existing.Name = action.Name;
        existing.Target = action.Target;
        existing.WorkingDirectory = action.WorkingDirectory;
        existing.ActionType = action.ActionType;
        existing.UpdatedAtUtc = now;

        action.UpdatedAtUtc = now;
        await TouchWorkspaceAsync(dbContext, existing.WorkspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateNoteAsync(WorkspaceNote note)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.WorkspaceNotes.FindAsync(note.Id);

        if (existing is null)
            return;

        var now = DateTime.UtcNow;
        existing.Text = note.Text;
        existing.Type = note.Type;
        existing.UpdatedAtUtc = now;

        note.UpdatedAtUtc = now;
        await TouchWorkspaceAsync(dbContext, existing.WorkspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteActionAsync(WorkspaceAction action)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.WorkspaceActions.FindAsync(action.Id);

        if (existing is null)
            return;

        var now = DateTime.UtcNow;
        dbContext.WorkspaceActions.Remove(existing);
        await TouchWorkspaceAsync(dbContext, existing.WorkspaceId, now);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddActionRunAsync(WorkspaceAction action, WorkspaceActionRun run)
    {
        var finishedAtUtc = run.FinishedAtUtc ?? DateTime.UtcNow;

        run.WorkspaceId = action.WorkspaceId;
        run.WorkspaceActionId = action.Id;
        run.ActionNameSnapshot = action.Name;
        run.TargetSnapshot = action.Target;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.ActionRuns.Add(run);

        var existingAction = await dbContext.WorkspaceActions.FindAsync(action.Id);
        if (existingAction is not null)
        {
            existingAction.LastRunAtUtc = finishedAtUtc;
            existingAction.UpdatedAtUtc = finishedAtUtc;
        }

        await TouchWorkspaceAsync(dbContext, action.WorkspaceId, finishedAtUtc);
        await dbContext.SaveChangesAsync();

        action.LastRunAtUtc = finishedAtUtc;
        action.UpdatedAtUtc = finishedAtUtc;
    }

    public async Task<List<WorkspaceActionRun>> LoadActionRunsAsync(int workspaceId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.ActionRuns
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync();
    }

    private static async Task TouchWorkspaceAsync(AppDbContext dbContext, int workspaceId, DateTime updatedAtUtc)
    {
        var workspace = await dbContext.Workspaces.FindAsync(workspaceId);

        if (workspace is not null)
            workspace.UpdatedAtUtc = updatedAtUtc;
    }
    
    public async Task DeleteActionRunAsync(WorkspaceActionRun action)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var existing = await dbContext.ActionRuns.FindAsync(action.Id);
        
        if (existing is null)
            return;
        
        dbContext.ActionRuns.Remove(existing);
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearActionRunsAsync(int workspaceId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var runs = dbContext.ActionRuns
            .Where(x => x.WorkspaceId == workspaceId);
        
        dbContext.ActionRuns.RemoveRange(runs);
        await dbContext.SaveChangesAsync();
    }
}
