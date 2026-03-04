using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HospitalSurgical.Infrastructure.Interceptors;

/// <summary>
/// SaveChanges interceptor that automatically sets shadow property audit fields.
///
/// WHAT IS A SHADOW PROPERTY?
/// A shadow property exists in EF Core's metadata model but NOT as a C# property on the entity.
/// It is stored in the database but accessed only via EF Core's ChangeTracker.
/// This keeps domain entities clean of infrastructure concerns (CreatedAt, UpdatedBy, etc.)
///
/// HOW TO DEFINE DEFAULT VALUES for shadow properties:
///   builder.Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
///   OR set in interceptor as shown below.
///
/// HOW TO QUERY shadow properties:
///   _context.Entry(entity).Property("CreatedAt").CurrentValue
///   OR in LINQ: .OrderBy(e => EF.Property<DateTime>(e, "CreatedAt"))
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var currentUser = _currentUserService.UserName ?? "System";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip owned entities and entities without audit fields
            if (!HasAuditProperties(entry)) continue;

            if (entry.State == EntityState.Added)
            {
                SetIfExists(entry, "CreatedAt", now);
                SetIfExists(entry, "UpdatedAt", now);
                SetIfExists(entry, "CreatedBy", currentUser);
                SetIfExists(entry, "UpdatedBy", currentUser);
            }
            else if (entry.State == EntityState.Modified)
            {
                SetIfExists(entry, "UpdatedAt", now);
                SetIfExists(entry, "UpdatedBy", currentUser);

                // Preserve Created fields if they exist
                SetNotModifiedIfExists(entry, "CreatedAt");
                SetNotModifiedIfExists(entry, "CreatedBy");
            }
        }
    }


    private static bool HasAuditProperties(EntityEntry entry)
    {
        var entityType = entry.Metadata;

        return entityType.FindProperty("CreatedAt") != null &&
               entityType.FindProperty("UpdatedAt") != null &&
               entityType.FindProperty("CreatedBy") != null &&
               entityType.FindProperty("UpdatedBy") != null;
    }

    private static void SetIfExists(EntityEntry entry, string propertyName, object value)
    {
        if (entry.Metadata.FindProperty(propertyName) != null)
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }

    private static void SetNotModifiedIfExists(EntityEntry entry, string propertyName)
    {
        if (entry.Metadata.FindProperty(propertyName) != null)
        {
            entry.Property(propertyName).IsModified = false;
        }
    }
}


/// <summary>
/// Service to get the currently authenticated user's identity.
/// In production: inject IHttpContextAccessor and read User.Identity.Name
/// </summary>
public interface ICurrentUserService
{
    string? UserName { get; }
}


/// <summary>Development implementation — returns a fixed user.</summary>
public class CurrentUserService : ICurrentUserService
{
    public string? UserName => "dev-user";
}