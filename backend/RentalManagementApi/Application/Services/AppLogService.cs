using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Logs;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class AppLogService(AppDbContext db)
{
    /// Adds a log entry to the EF context. Caller must call SaveChangesAsync.
    public void Log(Guid userId, string action, string entityType, Guid? entityId, string description)
    {
        db.AppLogs.Add(new AppLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description
        });
    }

    public async Task<PagedLogsResult> GetLogsAsync(Guid userId, string? entityType, int page, int pageSize)
    {
        var query = db.AppLogs.Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AppLogDto
            {
                Id = l.Id,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new PagedLogsResult { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
