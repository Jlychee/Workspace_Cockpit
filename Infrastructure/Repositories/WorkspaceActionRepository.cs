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
        var maxSortOrder = await dbContext.WorkspaceActions
            .Where(x => x.WorkspaceId == workspaceId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? -1;

        action.SortOrder = maxSortOrder + 1;
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

    public async Task UpdateActionOrderAsync(int workspaceId, IReadOnlyList<WorkspaceAction> orderedActions)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var actionIds = orderedActions.Select(x => x.Id).ToList();
        var existingActions = await dbContext.WorkspaceActions
            .Where(x => x.WorkspaceId == workspaceId && actionIds.Contains(x.Id))
            .ToListAsync();
        var existingById = existingActions.ToDictionary(x => x.Id);

        for (var index = 0; index < orderedActions.Count; index++)
        {
            var action = orderedActions[index];
            action.SortOrder = index;

            if (existingById.TryGetValue(action.Id, out var existing))
                existing.SortOrder = index;
        }

        workspaceTimestampService.Update(dbContext, workspaceId);
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
