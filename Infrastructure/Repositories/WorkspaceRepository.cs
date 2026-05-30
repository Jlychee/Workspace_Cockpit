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
            .Include(x => x.Notes.OrderBy(note => note.SortOrder).ThenBy(note => note.Id))
            .Include(x => x.Actions.OrderBy(action => action.SortOrder).ThenBy(action => action.Id))
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
}
