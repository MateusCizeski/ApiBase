﻿using ApiBase.Core.Domain.Entities;
using ApiBase.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ApiBase.Core.Infra.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly DbContext _dbContext;
        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public void Commit()
        {
            try
            {
                ValidateEntities();
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                RollbackChanges();
                throw new Exception("Database update failed: " + ex.GetBaseException().Message, ex);
            }
            catch (ValidationException ex)
            {
                RollbackChanges();
                throw new Exception("Validation failed: " + ex.Message, ex);
            }
        }

        private void ValidateEntities()
        {
            var entitiesToValidate = _dbContext.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity);

            foreach (var entity in entitiesToValidate)
            {
                var validationContext = new ValidationContext(entity);
                Validator.ValidateObject(entity, validationContext, validateAllProperties: true);
            }
        }

        public void RollbackChanges()
        {
            var changedEntries = _dbContext.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList();

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }

        public IList<object> BuildCustomFieldsList<T>(List<object> pagedResults) where T : EntityGuid, new()
        {
            return pagedResults.Select(obj => MergeCustomFields<T>(obj)).ToList();
        }

        public object BuildCustomFieldsList<T>(object result) where T : EntityGuid, new()
        {
            return MergeCustomFields<T>(result);
        }

        private object MergeCustomFields<T>(object source) where T : EntityGuid, new()
        {
            var entityProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();

            if (source is IDictionary<string, object> dict)
            {
                var result = new Dictionary<string, object>(dict);

                return result;
            }

            var target = new Dictionary<string, object>();
            var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                target[prop.Name] = prop.GetValue(source);
            }

            return target;
        }
    }
}
