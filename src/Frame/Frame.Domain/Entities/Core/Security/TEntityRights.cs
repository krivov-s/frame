
namespace Frame.Domain.Entities.Core.Security
{
    /// <summary>
    /// Права доступа к модели с типом EntityTypeName для роли Role.
    /// <para>
    /// TLS = Type Level Security, OLS = Object Level Security
    /// </para>
    /// </summary>
    public class TEntityRights : BaseEntity, IBaseGenericEntity<TEntityRights>
    {
        public string EntityTypeName { get; set; } = "";


        #region ========== TLS: Type level security ==========

        public string CreateTLS { get; set; } = "";
        public string CreateOLS { get; set; } = "";
        public string ReadQueryFilter { get; set; } = "";
        public string ViewTLS { get; set; } = "";
        public string ModifyTLS { get; set; } = "";
        
        // Права на копирование представляются избыточными. Это всего лишь производная от Create.
        public string CopyTLS { get; set; } = "";
        public string DeleteTLS { get; set; } = "";
        public string AuditTLS { get; set; } = "";

        #endregion

        #region ========== OLS: Object level security ==========

        public string ViewOLS { get; set; } = "";
        public string ReadOLS { get; set; } = "";
        public string ModifyOLS { get; set; } = "";
        
        // Права на копирование представляются избыточными. Это всего лишь производная от Create.
        public string CopyOLS { get; set; } = "";
        public string DeleteOLS { get; set; } = "";
        public string AuditOLS { get; set; } = "";
        #endregion

        #region ========== ALS: Attr level security ==========

        /// <summary>
        /// По-умолчанию (когда <see cref="CanReadAttrs"/> == true) - список полей, разрешенных для чтения 
        /// </summary>
        public string ReadAttrs { get; set; } = "";
        public bool CanReadAttrs { get; set; } = true;
        
        /// <summary>
        /// По-умолчанию (когда <see cref="CanModifyAttrs"/> == true) - список полей, разрешенных для изменения 
        /// </summary>
        public string ModifyAttrs { get; set; } = "";
        public bool CanModifyAttrs { get; set; } = true;

        #endregion

        #region ========== User interface security ==========

        public string Print { get; set; } = "";
        public string PrintPreview { get; set; } = "";
        public string SaveAsTemplate { get; set; } = "";
        public string CreateFromTemplate { get; set; } = "";
        public string Export { get; set; } = "";

        #endregion
        
        
        
        public virtual Role? Role { get; set; }
        public int? RoleId { get; set; }

        public List<Field<TEntityRights>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Ограничения на тип для роли";
            
            public static readonly Field<TEntityRights> EntityTypeName = new()
            {
                Name = nameof(EntityTypeName), 
                StringGet = x => x.EntityTypeName,
                StringSet = (x, val) => x.EntityTypeName = val ?? "",
                Required = true, 
                MaxLength = 80, 
                HumanName = "Наименование типа"
            };
            public static readonly Field<TEntityRights> CreateTLS = new()
            {
                Name = nameof(CreateTLS), 
                StringGet = x => x.CreateTLS,
                StringSet = (x, val) => x.CreateTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на создание (TLS)"
            };
            public static readonly Field<TEntityRights> CreateOLS = new()
            {
                Name = nameof(CreateOLS), 
                StringGet = x => x.CreateOLS, 
                StringSet = (x, val) => x.CreateOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на создание (OLS)"
            };
            public static readonly Field<TEntityRights> ReadQueryFilter = new()
            {
                Name = nameof(ReadQueryFilter), 
                StringGet = x => x.ReadQueryFilter, 
                StringSet = (x, val) => x.ReadQueryFilter = val ?? "",
                Required = false, 
                MaxLength = 1024, 
                HumanName = "Фильтр на чтение"
            };
            public static readonly Field<TEntityRights> ViewTLS = new()
            {
                Name = nameof(ViewTLS), 
                StringGet = x => x.ViewTLS, 
                StringSet = (x, val) => x.ViewTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на просмотр (TLS)"
            };
            public static readonly Field<TEntityRights> ViewOLS = new()
            {
                Name = nameof(ViewOLS), 
                StringGet = x => x.ViewOLS, 
                StringSet = (x, val) => x.ViewOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на просмотр (OLS)"
            };
            public static readonly Field<TEntityRights> ModifyTLS = new()
            {
                Name = nameof(ModifyTLS), 
                StringGet = x => x.ModifyTLS,
                StringSet = (x, val) => x.ModifyTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на редактирование (TLS)"
            };
            public static readonly Field<TEntityRights> ModifyOLS = new()
            {
                Name = nameof(ModifyOLS), 
                StringGet = x => x.ModifyOLS, 
                StringSet = (x, val) => x.ModifyOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на редактирование (OLS)"
            };
            public static readonly Field<TEntityRights> CopyTLS = new()
            {
                Name = nameof(CopyTLS), 
                StringGet = x => x.CopyTLS, 
                StringSet = (x, val) => x.CopyTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на копирование (TLS)"
            };
            public static readonly Field<TEntityRights> CopyOLS = new()
            {
                Name = nameof(CopyOLS), 
                StringGet = x => x.CopyOLS, 
                StringSet = (x, val) => x.CopyOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на копирование (OLS)"
            };
            public static readonly Field<TEntityRights> DeleteTLS = new()
            {
                Name = nameof(DeleteTLS), 
                StringGet = x => x.DeleteTLS, 
                StringSet = (x, val) => x.DeleteTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на удаление (TLS)"
            };
            public static readonly Field<TEntityRights> DeleteOLS = new()
            {
                Name = nameof(DeleteOLS), 
                StringGet = x => x.DeleteOLS, 
                StringSet = (x, val) => x.DeleteOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на удаление (OLS)"
            };
            public static readonly Field<TEntityRights> AuditTLS = new()
            {
                Name = nameof(AuditTLS), 
                StringGet = x => x.AuditTLS,
                StringSet = (x, val) => x.AuditTLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на аудит (TLS)"
            };
            public static readonly Field<TEntityRights> AuditOLS = new()
            {
                Name = nameof(AuditOLS), 
                StringGet = x => x.AuditOLS,
                StringSet = (x, val) => x.AuditOLS = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Условие на аудит (OLS)"
            };
            public static readonly Field<TEntityRights> Print = new()
            {
                Name = nameof(Print), 
                StringGet = x => x.Print,
                StringSet = (x, val) => x.Print = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Печать"
            };
            public static readonly Field<TEntityRights> PrintPreview = new()
            {
                Name = nameof(PrintPreview), 
                StringGet = x => x.PrintPreview,
                StringSet = (x, val) => x.PrintPreview = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Просмотр"
            };
            public static readonly Field<TEntityRights> SaveAsTemplate = new()
            {
                Name = nameof(SaveAsTemplate), 
                StringGet = x => x.SaveAsTemplate,
                StringSet = (x, val) => x.SaveAsTemplate = val ?? "",
                Required = false, 
                MaxLength = 512,
                HumanName = "Сохранить как шаблон"
            };
            public static readonly Field<TEntityRights> CreateFromTemplate = new()
            {
                Name = nameof(CreateFromTemplate), 
                StringGet = x => x.CreateFromTemplate, 
                StringSet = (x, val) => x.CreateFromTemplate = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Создать по шаблону"
            };
            public static readonly Field<TEntityRights> Export = new()
            {
                Name = nameof(Export), 
                StringGet = x => x.Export, 
                StringSet = (x, val) => x.Export = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Экспорт"
            };
            public static readonly Field<TEntityRights> ModifyAttrs = new()
            {
                Name = nameof(ModifyAttrs), 
                StringGet = x => x.ModifyAttrs, 
                StringSet = (x, val) => x.ModifyAttrs = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Поля редактирования",
                HelperText = "Список полей через запятую, которые доступны/недоступны для чтения и редактирования"
            };
            public static readonly Field<TEntityRights> CanModifyAttrs = new()
            {
                Name = nameof(CanModifyAttrs), 
                BoolGet = x => x.CanModifyAttrs, 
                BoolSet = (x, val) => x.CanModifyAttrs = val ?? false,
                Required = true, 
                HumanName = "Доступны для редактирования",
                HelperText = "Если установлена - только те поля что в списке можно редактировать, а остальные нельзя, " +
                             "если нет - только те поля что в списке нельзя редактировать, а остальные - можно."
            };
            public static readonly Field<TEntityRights> ReadAttrs = new()
            {
                Name = nameof(ReadAttrs), 
                StringGet = x => x.ReadAttrs, 
                StringSet = (x, val) => x.ReadAttrs = val ?? "",
                Required = false, 
                MaxLength = 512, 
                HumanName = "Поля только для чтения",
                HelperText = "Список полей через запятую, которые доступны/недоступны для чтения"
            };
            public static readonly Field<TEntityRights> CanReadAttrs = new()
            {
                Name = nameof(CanReadAttrs), 
                BoolGet = x => x.CanReadAttrs, 
                BoolSet = (x, val) => x.CanReadAttrs = val ?? false,
                Required = true, 
                HumanName = "Доступны для чтения",
                HelperText = "Если установлена - только те поля что в списке можно читать, а остальные нельзя, " +
                             "если нет - только те поля что в списке нельзя читать, а остальные - можно."
            };

            public static readonly List<Field<TEntityRights>> Fields = [EntityTypeName, CreateTLS, CreateOLS, 
                ReadQueryFilter, ViewTLS, ViewOLS, ModifyTLS, ModifyOLS, CopyTLS, CopyOLS, DeleteTLS, DeleteOLS, 
                AuditTLS, AuditOLS, Print, PrintPreview, SaveAsTemplate, CreateFromTemplate, Export, 
                ModifyAttrs, CanModifyAttrs, ReadAttrs, CanReadAttrs];
        }
    }
}
