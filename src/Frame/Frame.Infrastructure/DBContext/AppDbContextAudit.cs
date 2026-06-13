using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Frame.App.Security;
using Frame.Shared;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Security;

namespace Frame.Infrastructure.DBContext
{
    public partial class AppDbContext 
    {
        /// <summary>
        /// Флаг, указывающий режим аудита. Если true - для объектов, наследующих от класса <see cref="BaseEntity"/> автоматически при  
        /// сохранениив в БД будет осуществляться формирование и запись аудита - объектов <see cref="Domain.Entities.Core.Security.AuditRecord"/>. По умолчанию = true (аудит ведется)
        /// </summary>
        public bool AuditMode { get; set; } = true;
        public DbSet<AuditRecord> AuditRecord { get; set; }

        async Task<IEnumerable<Tuple<EntityEntry, AuditRecord>>> AuditNonTemporaryProperties()
        {
            string strUserName = _userCore.CurrentUserLogin;
            ChangeTracker.DetectChanges();
            IEnumerable<EntityEntry> entitiesToTrack = base.ChangeTracker.Entries().Where(
                                                                e => e.Entity is not Domain.Entities.Core.Security.AuditRecord
                                                                && e.State != EntityState.Detached
                                                                && e.State != EntityState.Unchanged
                                                                && e.Entity is BaseEntity);

            // Список объектов у которых ОТСУТСТВУЮТ временные свойства (p.IsTemporary == false) сохраняем в коллекцию AuditRecords
            IEnumerable<EntityEntry> nonTempEntities = entitiesToTrack.Where(e => !e.Properties.Any(p => p.IsTemporary));

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };        
                 
            await AuditRecord.AddRangeAsync(nonTempEntities.Select(
                e => new AuditRecord()
                {
                    RecordDateTime = DateTime.UtcNow,
                    UserName = strUserName,
                    EntityType = e.Metadata.ClrType.Name,
                    EntityId = ServiceTools.ConvertToInt(e.Property("Id").CurrentValue),
                    Action = Enum.GetName(typeof(EntityState), e.State)!,
                    KeyValues = JsonSerializer.Serialize(e.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), options),
                    NewValues = JsonSerializer.Serialize(e.Properties.Where(_ => e.State == EntityState.Added || e.State == EntityState.Modified).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), options),
                    OldValues = JsonSerializer.Serialize(e.Properties.Where(_ => e.State == EntityState.Deleted || e.State == EntityState.Modified).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue), options),
                    Changes = JsonSerializer.Serialize(e.GetChangedPropValues(), options)
                }).ToList());


            // Список объектов (карту Tuple<EntityEntry, AuditRecord), у которых ПРИСУТСТВУЮТ временные свойства (p.IsTemporary == true)
            // возвращаем, для переиспользования после сохранения и получения окончательных значений свойств
            IEnumerable<EntityEntry> temporaryEntities = entitiesToTrack.Where(e => e.Properties.Any(p => p.IsTemporary));
            List<Tuple<EntityEntry, AuditRecord>> mapEntityToAudit = temporaryEntities.Select(
                entityEntry => new Tuple<EntityEntry, AuditRecord>(
                     entityEntry, new AuditRecord()
                     {
                         RecordDateTime = DateTime.UtcNow,
                         UserName = strUserName,
                         EntityType = entityEntry.Metadata.ClrType.Name,
                         EntityId = ServiceTools.ConvertToInt(entityEntry.Property("Id").CurrentValue),
                         Action = Enum.GetName(typeof(EntityState), entityEntry.State)!,
                         NewValues = JsonSerializer.Serialize(entityEntry.Properties.Where(p => !p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), options)
                     })).ToList();

            return mapEntityToAudit;
        }

        async Task AuditTemporaryProperties(IEnumerable<Tuple<EntityEntry, AuditRecord>> temporatyEntities)
        {
            if (temporatyEntities != null && temporatyEntities.Any())
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };        
                 
                foreach (var entityMapRecord in temporatyEntities)
                {
                    try
                    {
                        BaseEntity entityBase = (BaseEntity)entityMapRecord.Item1.Entity;
                        AuditRecord auditRecord = entityMapRecord.Item2;
                        auditRecord.EntityId = entityBase.Id;
                        auditRecord.KeyValues = JsonSerializer.Serialize(entityMapRecord.Item1.Properties
                                                                            .Where(p => p.Metadata.IsPrimaryKey())
                                                                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue), options);
                        AuditRecord.Add(auditRecord);
                    }
                    catch (Exception)
                    {
                        // Это очень странная ошибка преобразования: в иделе не должна возникнуть никогда,
                        // поскольку аудит работает только с объектами <see cref="BaseEntity"/>
                        // Просто пропускаем данный объект.
                    }
                }
                await SaveChangesAsync();
            }
            await Task.CompletedTask;
        }

    }
}
