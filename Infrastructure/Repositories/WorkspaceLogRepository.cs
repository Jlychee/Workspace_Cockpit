using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Infrastructure.Repositories;

public class WorkspaceLogRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    WorkspaceTimestampService workspaceTimestampService)
{
    public async Task AddLogAsync(WorkspaceAction action, WorkspaceLog run)
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

        workspaceTimestampService.Update(dbContext, action.WorkspaceId, finishedAtUtc);

        await dbContext.SaveChangesAsync();

        action.LastRunAtUtc = finishedAtUtc;
        action.UpdatedAtUtc = finishedAtUtc;
    }

    public async Task<List<WorkspaceLog>> LoadLogsAsync(int workspaceId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.ActionRuns
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync();
    }

    public async Task DeleteLogAsync(WorkspaceLog action)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var existing = await dbContext.ActionRuns.FindAsync(action.Id);

        if (existing is null)
            return;

        dbContext.ActionRuns.Remove(existing);
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearLogsAsync(int workspaceId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var runs = dbContext.ActionRuns
            .Where(x => x.WorkspaceId == workspaceId);

        dbContext.ActionRuns.RemoveRange(runs);
        await dbContext.SaveChangesAsync();
    }
}