using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Test;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Metadata;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Frame.Shared;

namespace Frame.Infrastructure.DBContext
{
    public partial class AppDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var  type in EntityMetadata.GetAllEntitiesClassTypes())
            {
                modelBuilder.Entity(type);
            }

            // Защита от concurrency updates для Postgres
            if (IsPostgres())
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property<uint>("xmin") // shadow property
                            .IsRowVersion()
                            .HasColumnName("xmin");
                    }
                }    
            }
            else
            {
                string err =
                    "ВНИМАНИЕ! ДЛЯ ДАННОГО ТИПА БД НЕ СКОНФИГУРИРОВАН МЕХАНИЗМ ЗАЩИТЫ ОТ ПЕРЕЗАПИСИ ДАННЫХ (CONCURRENCY CONTROL)!";
                _logger.LogWarning(err, Result.Success);
            }
            
            // Assembly assembly = Assembly.GetExecutingAssembly();
            // modelBuilder.ApplyConfigurationsFromAssembly(assembly);

            // Получаем список сборок, в названии которых присутствует имя Infrastructure
            var listAssemblies = ServiceTools.GetAssemblies(".Infrastructure");
            // var listAssemblies = AppDomain.CurrentDomain
            //     .GetAssemblies()
            //     .Where(a => a.GetName().Name.Contains("Infrastructure"));
            
            foreach (var dll in listAssemblies)
            { 
                modelBuilder.ApplyConfigurationsFromAssembly(dll);
            }
            
            #region Для SQL-сервера принудительно устанавливаем точность decimals 18.2

            if (IsSqlServer())
            {
                foreach (var tt in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var prop in tt.GetProperties())
                    {
                        if (prop.ClrType.Name.ToUpper().Contains("DECIMAL"))
                        {
#if DEBUG
                            Debug.WriteLine("\tMSSQL: Set decimal precision for " + prop.Name);
#endif
                            prop.SetColumnType("decimal(18, 2)");
                        }
                    }
                }
            }
            #endregion
            
            base.OnModelCreating(modelBuilder);
        }

        #region ========== Обработка OnBefore/After/Save/Delete ========== 

        public List<EntityEntry> GetQueue() => base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && (
                                                                                            e.State == EntityState.Added || 
                                                                                            e.State == EntityState.Modified || 
                                                                                            e.State == EntityState.Deleted)
                                                                                  ).ToList();

        //public List<EntityEntry> GetChanged() => base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && (e.State == EntityState.Added || e.State == EntityState.Modified)).ToList();
        //public List<EntityEntry> GetDeleted() => base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && e.State == EntityState.Deleted).ToList();

        // private readonly List<EntityEntry> _eeAddedList = [];
        // private readonly List<EntityEntry> _eeModifiedList = [];
        // private readonly List<EntityEntry> _eeDeletedList = [];
        //
        //
        // protected void InitEntitiesList()
        // {
        //     _eeAddedList.Clear();
        //     _eeModifiedList.Clear();
        //     _eeDeletedList.Clear();
        //     ChangeTracker.DetectChanges();
        //
        //     _eeAddedList.AddRange(base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && e.State == EntityState.Added));
        //     _eeModifiedList.AddRange(base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && e.State == EntityState.Modified));
        //     _eeDeletedList.AddRange(base.ChangeTracker.Entries().Where(e => e.Entity is not Domain.Entities.Core.Security.AuditRecord && e.State == EntityState.Deleted));
        // }
        //
        //protected async Task CallOnBeforeSaveAsync()
        //{
        //    await CallFunctionSyncAsync(eeAddedList,
        //                                (entity) => entity.OnBeforeSave(_appCore),
        //                                async (entity) => await entity.OnBeforeSaveAsync(_appCore) );

        //    await CallFunctionSyncAsync(eeModifiedList, 
        //                                (entity) => entity.OnBeforeSave(_appCore),
        //                                async (entity) => await entity.OnBeforeSaveAsync(_appCore));
        //}

        //protected async Task CallOnBeforeDeleteAsync()
        //{
        //    await CallFunctionSyncAsync(eeDeletedList, 
        //                                (entity) => entity.OnBeforeDelete(_appCore),
        //                                async (entity) => await entity.OnBeforeDeleteAsync(_appCore));
        //}

        //protected async Task CallOnAfterSaveAsync()
        //{
        //    await CallActionSyncAsync(eeAddedList, 
        //                              (entity) => entity.OnAfterSave(_appCore),
        //                              async (entity) => await entity.OnAfterSaveAsync(_appCore));

        //    await CallActionSyncAsync(eeModifiedList, 
        //                              (entity) => entity.OnAfterSave(_appCore),
        //                              async (entity) => await entity.OnAfterSaveAsync(_appCore));
        //}

        //protected async Task CallOnAfterDeleteAsync()
        //{
        //    await CallActionSyncAsync(eeDeletedList, 
        //                              (entity) => entity.OnAfterDelete(_appCore),
        //                              async (entity) => await entity.OnAfterDeleteAsync(_appCore));
        //}

        //private static async Task CallFunctionSyncAsync(List<EntityEntry> entityEntries, 
        //                                                Func<BaseEntity, Result> funcSync, 
        //                                                Func<BaseEntity, Task<Result>> funcAsync)
        //{
        //    foreach (EntityEntry ee in entityEntries)
        //    {
        //        BaseEntity? baseEntity = ee.Entity as BaseEntity;
        //        if (baseEntity != null)
        //        {
        //            Result resSync = funcSync(baseEntity);
        //            if (resSync.IsError)
        //            {
        //                throw new FrameEntityException(resSync.ErrorResult);
        //            }

        //            Result resAsync = await funcAsync(baseEntity);
        //            if (resAsync.IsError)
        //            {
        //                throw new FrameEntityException(resSync.ErrorResult);
        //            }
        //        }
        //    }
        //}

        //private static async Task CallActionSyncAsync(  List<EntityEntry> entityEntries, 
        //                                                Action<BaseEntity> actionSync,
        //                                                Func<BaseEntity, Task> actionAsync )
        //{
        //    foreach (EntityEntry ee in entityEntries)
        //    {
        //        BaseEntity? baseEntity = ee.Entity as BaseEntity;
        //        if (baseEntity != null)
        //        {
        //            actionSync(baseEntity);
        //            await actionAsync(baseEntity);
        //        }
        //    }
        //}

        #endregion

        #region Public models (в обход Security)
        public DbSet<TestObject> TestObject { get; set; }
        public DbSet<TestChildObject> TestChildObject { get; set; }
        // public DbSet<AuditRecord> AuditRecords { get; set; }

        // -----------------------------------------------------------------------------------------------
        // Модели безопасности добавлены сюда в обход security только для того, чтобы нормально работали
        // EFUserRepository и EFRoleRepository, которые должны считывать пользователей и роли в обход
        // security, поскольку это собственно и нужно для инициализации security текущего пользователя
        // NoSecurityContext не стал использовать чтобы не вводить излишних сложностей в EFBaseRepository,
        // не учить его работать с двумя типами контекстов, не делать на каждом запросе DBSet-а проверку
        // с каким контекстом в данный момент работаем.

        // ХЕРНЯ!!!! В NoSecurityDbContext можно также переопределить метод GetSet и все будет просто!!!
        // Сделать интерфейс и его реализацию двумя типами контекстов.
        // ЛОШАРА!!!!!!!!
        // -----------------------------------------------------------------------------------------------
        //public DbSet<User> Users { get; set; }
        //public DbSet<Role> Roles { get; set; }
        //public DbSet<UsersRoles> UsersRoles { get; set; }
        //public DbSet<UserClaim> UserClaims { get; set; }
        // -----------------------------------------------------------------------------------------------
        #endregion

        #region Private models (получить можно только через GetSet)
        // --------------------------------------------------------------------------------------------------------------
        // Все модели, которые должны подчинены контексту с учетом настроек безопасности, перечисляются здесь как private
        // --------------------------------------------------------------------------------------------------------------
        #endregion

    }
}
