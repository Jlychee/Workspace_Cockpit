using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Infrastructure.Repositories;

public class WorkspaceActionRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    WorkspaceTimestampService workspaceTimestampService)
{
    public async Task AddActionAsync(int workspaceId, WorkspaceAction action)
    {
        var now = DateTime.UtcNow;
        action.WorkspaceId = workspaceId;
        action.CreatedAtUtc = now;
        action.UpdatedAtUtc = now;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.WorkspaceActions.Add(action);
        workspaceTimestampService.Update(dbContext, workspaceId, DateTime.UtcNow);
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
        workspaceTimestampService.Update(dbContext, existing.WorkspaceId);
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
        workspaceTimestampService.Update(dbContext, existing.WorkspaceId);
        await dbContext.SaveChangesAsync();
    }
}