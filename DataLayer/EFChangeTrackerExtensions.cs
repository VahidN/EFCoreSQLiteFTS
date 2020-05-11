using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCoreSQLiteFTS.DataLayer
{
    public static class EFChangeTrackerExtensions
    {
        public static List<(EntityState State, TEntity NewEntity, TEntity OldEntity)>
                    GetChangedEntities<TEntity>(this DbContext dbContext) where TEntity : class, new()
        {
            if (!dbContext.ChangeTracker.AutoDetectChangesEnabled)
            {
                // ChangeTracker.Entries() only calls `Try`DetectChanges() behind the scene.
                dbContext.ChangeTracker.DetectChanges();
            }

            return dbContext.ChangeTracker.Entries<TEntity>()
                    .Where(IsEntityChanged)
                    .Select(entityEntry => (entityEntry.State,
                                            entityEntry.Entity,
                                            createWithValues<TEntity>(entityEntry.OriginalValues)))
                    .ToList();
        }

        private static bool IsEntityChanged(EntityEntry entry)
        {
            return entry.State == EntityState.Added
                    || entry.State == EntityState.Modified
                    || entry.State == EntityState.Deleted
                    || entry.References.Any(r => r.TargetEntry?.Metadata.IsOwned() == true && IsEntityChanged(r.TargetEntry));
        }

        private static T createWithValues<T>(PropertyValues values) where T : new()
        {
            var entity = new T();
            foreach (var prop in values.Properties)
            {
                var value = values[prop.Name];
                if (value is PropertyValues)
                {
                    throw new NotSupportedException("nested complex object");
                }
                else
                {
                    prop.PropertyInfo.SetValue(entity, value);
                }
            }
            return entity;
        }
    }
}