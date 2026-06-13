using System.ComponentModel.DataAnnotations;

namespace Frame.Domain.Entities.Core.Security
{
    public class AuditRecord : BaseEntity, IBaseGenericEntity<AuditRecord>
    {
        //[FileKey, Column(Order = 0)]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime RecordDateTime { get; set; }
        public string UserName { get; set; } = "";
        /// <summary>
        /// Имя объекта в системе
        /// </summary>
        public string EntityType { get; set; } = "";
        /// <summary>
        /// Идентификатор объекта в БД
        /// </summary>
        [Required]
        public int EntityId { get; set; } = 0;
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = "";
        public string KeyValues { get; set; } = "";
        public string OldValues { get; set; } = "";
        public string NewValues { get; set; } = "";
        public string Changes { get; set; } = "";
        public override string Description => $"{EntityType} {Action} {UserName} {RecordDateTime}";

        public List<Field<AuditRecord>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Запись аудита";
            public static readonly Field<AuditRecord> RecordDateTime = new() { Name = nameof(RecordDateTime), DateTimeGet = x => x.RecordDateTime, Required = true, HumanName = "Дата/время записи" };
            public static readonly Field<AuditRecord> UserName = new() { Name = nameof(UserName), StringGet = x => x.UserName, Required = true, MaxLength = 255, HumanName = "Имя пользователя" };
            public static readonly Field<AuditRecord> EntityType = new() { Name = nameof(EntityType), StringGet = x => x.EntityType, Required = true, MaxLength = 255, HumanName = "Тип объекта" };
            public static readonly Field<AuditRecord> EntityId = new() { Name = nameof(EntityId), IntGet = x => x.EntityId, Required = true, HumanName = "Id объекта" };
            public static readonly Field<AuditRecord> Action = new() { Name = nameof(Action), StringGet = x => x.Action, Required = true, MaxLength = 255, HumanName = "Действие" };
            public static readonly Field<AuditRecord> KeyValues = new() { Name = nameof(KeyValues), StringGet = x => x.KeyValues, Required = false, MaxLength = 1024, HumanName = "Значения ключа" };
            public static readonly Field<AuditRecord> OldValues = new() { Name = nameof(OldValues), StringGet = x => x.OldValues, Required = false, MaxLength = 1024, HumanName = "Все старые значения" };
            public static readonly Field<AuditRecord> NewValues = new() { Name = nameof(NewValues), StringGet = x => x.NewValues, Required = false, MaxLength = 1024, HumanName = "Все новые значения" };
            public static readonly Field<AuditRecord> Changes = new() { Name = nameof(Changes), StringGet = x => x.Changes, Required = false, MaxLength = 1024, HumanName = "Изменения" };

            public static readonly List<Field<AuditRecord>> Fields = [RecordDateTime, UserName, EntityType, EntityId, Action, KeyValues, OldValues, NewValues, Changes];
        }
    }
}
