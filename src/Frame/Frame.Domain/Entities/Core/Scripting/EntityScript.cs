
namespace Frame.Domain.Entities.Core.Scripting
{
    public class EntityScript : BaseEntity, IBaseGenericEntity<EntityScript>
    {
        public override string Description => Meta.BuildScriptClassName(this);

        public string EntityType { get; set; } = "";
        public EHookType HookType { get; set; } = EHookType.OnBeforeSave;
        public ECodeType CodeType { get; set; } = ECodeType.SimpleMethod;
        public EThreadType ThreadType { get; set; } = EThreadType.Sync;
        public string ScriptCode { get; set; } = "";

        public List<Field<EntityScript>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Entity script-hook";
            public static readonly Field<EntityScript> EntityType = new() { Name = nameof(EntityType), StringGet = x => x.EntityType, StringSet = (x, val) => x.EntityType = val ?? "", Required = true, MaxLength = 255, HumanName = "Тип объекта" };
            public static readonly Field<EntityScript> HookType = new()
            {
                Name = nameof(HookType),
                IntGet = x => (int)x.HookType,
                IntSet = (x, val) => x.HookType = (val == null) ? EHookType.OnBeforeSave : (EHookType)val,
                IntValues = ESelectListItems.HookTypes,
                Required = true,
                HumanName = "Тип скрипта"
            };
            public static readonly Field<EntityScript> CodeType = new()
            {
                Name = nameof(CodeType),
                IntGet = x => (int)x.CodeType,
                IntSet = (x, val) => x.CodeType = (val == null) ? ECodeType.SimpleMethod : (ECodeType)val,
                IntValues = ESelectListItems.CodeTypes,
                Required = true,
                HumanName = "Тип кода"
            };
            public static readonly Field<EntityScript> ThreadType = new()
            {
                Name = nameof(ThreadType),
                IntGet = x => (int)x.ThreadType,
                IntSet = (x, val) => x.ThreadType = (val == null) ? EThreadType.Sync : (EThreadType)val,
                IntValues = ESelectListItems.ThreadTypes,
                Required = true,
                HumanName = "Тип кода"
            };
            public static readonly Field<EntityScript> ScriptCode = new() { Name = nameof(ScriptCode), StringGet = x => x.ScriptCode, StringSet = (x, val) => x.ScriptCode = val ?? "", Required = true, MaxLength = 4096, HumanName = "Код c#" };

            public static readonly List<Field<EntityScript>> Fields = [EntityType, HookType, CodeType, ScriptCode];

            public static string BuildScriptClassName(BaseEntity entity, EHookType hookType)
            {
                string strEntityClassType = entity.GetType().Name;
                return _buildClassName(hookType, strEntityClassType);
            }
            public static string BuildScriptClassName<TEntity>(EHookType hookType)
            {
                string strEntityClassType = typeof(TEntity).Name;
                return _buildClassName(hookType, strEntityClassType);
            }
            public static string BuildScriptClassName(EntityScript entityScript)
            {
                string strEntityClassType = entityScript.EntityType;
                return _buildClassName(entityScript.HookType, strEntityClassType);
            }
            private static string _buildClassName(EHookType hookType, string strEntityClassType)
            {
                string strHookName = Enum.GetName(hookType) ?? "Некорректное значение EHookType";
                return $"{strEntityClassType}_{strHookName}";
            }


            ///// <summary>
            ///// Коллекция всех зарегистрированных хуков 
            ///// </summary>
            //public static readonly List<SelectListItem> HookListItems = Enum.GetValues<EHookType>()
            //        .Select(ee => new SelectListItem() 
            //                        { 
            //                            Value = (int)ee, 
            //                            Description = Enum.GetName(ee) ?? "Некорректное значение EHookType"
            //                        })
            //        .ToList();

            ///// <summary>
            ///// Получение имени хука по его типу
            ///// </summary>
            //public static string GetHookName(EHookType hookType) => hookType switch
            //{
            //    EHookType.OnBeforeSave => nameof(EHookType.OnBeforeSave),
            //    EHookType.OnAfterSave => nameof(EHookType.OnAfterSave),
            //    EHookType.OnBeforeSaveAsync => nameof(EHookType.OnBeforeSaveAsync),
            //    EHookType.OnAfterSaveAsync => nameof(EHookType.OnAfterSaveAsync),
            //    EHookType.OnBeforeDelete => nameof(EHookType.OnBeforeDelete),
            //    EHookType.OnAfterDelete => nameof(EHookType.OnAfterDelete),
            //    EHookType.OnBeforeDeleteAsync => nameof(EHookType.OnBeforeDeleteAsync),
            //    EHookType.OnAfterDeleteAsync => nameof(EHookType.OnAfterDeleteAsync),
            //    _ => throw new Exception($"Передан неизвестный EntityScriptHookType: {hookType}")
            //};

            ///// <summary>
            ///// Список имен всех обрабатываемых хуков объектов
            ///// </summary>
            //public static readonly List<string> HookNames = [
            //    OnBeforeSave, OnAfterSave,
            //    OnBeforeSaveAsync, OnAfterSaveAsync,
            //    OnBeforeDelete, OnAfterDelete,
            //    OnBeforeDeleteAsync, OnAfterDeleteAsync];

            //public static readonly string OnBeforeSave = nameof(EHookType.OnBeforeSave);
            //public static readonly string OnAfterSave = nameof(EHookType.OnAfterSave);
            //public static readonly string OnBeforeSaveAsync = nameof(EHookType.OnBeforeSaveAsync);
            //public static readonly string OnAfterSaveAsync = nameof(EHookType.OnAfterSaveAsync);
            //public static readonly string OnBeforeDelete = nameof(EHookType.OnBeforeDelete);
            //public static readonly string OnAfterDelete = nameof(EHookType.OnAfterDelete);
            //public static readonly string OnBeforeDeleteAsync = nameof(EHookType.OnBeforeDeleteAsync);
            //public static readonly string OnAfterDeleteAsync = nameof(EHookType.OnAfterDeleteAsync);
        }
    }
}
