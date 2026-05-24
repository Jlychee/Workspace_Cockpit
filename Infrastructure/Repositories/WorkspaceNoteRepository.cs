using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Models.Entities;

namespace Infrastructure.Repositories;

public class WorkspaceNoteRepository(IDbContextFactory<AppDbContext> dbContextFactory, WorkspaceTimestampService workspaceTimestampService)
{
    public async Task AddNoteAsync(int workspaceId, WorkspaceNote note)
    {
        var now = DateTime.UtcNow;
        note.WorkspaceId = workspaceId;
        note.CreatedAtUtc = now;
        note.UpdatedAtUtc = now;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.WorkspaceNotes.Add(note);
        workspaceTimestampService.Update(dbContext, workspaceId);
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
        workspaceTimestampService.Update(dbContext, existing.WorkspaceId);
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
        workspaceTimestampService.Update(dbContext, existing.WorkspaceId);
        await dbContext.SaveChangesAsync();
    }
}