using Infrastructure.Data;

namespace Infrastructure.Services;

public class WorkspaceTimestampService
{
    public void Update(AppDbContext dbContext, int workspaceId, DateTime? updatedAtUtc = null)
    {
        var workspace = dbContext.Workspaces.Find(workspaceId);
        if (workspace is null)
            return;

        workspace.UpdatedAtUtc = updatedAtUtc ?? DateTime.UtcNow;
    }
}