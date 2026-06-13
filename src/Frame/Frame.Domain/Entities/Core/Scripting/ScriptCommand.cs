
using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Domain.Entities.Core.Queries;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Metadata;

namespace Frame.Domain.Entities.Core.Scripting
{
    public class ScriptCommand : BaseEntity, IBaseGenericEntity<ScriptCommand>
    {
        public override string Description => ScriptName;

        /// <summary>
        /// Наименование операции
        /// </summary>
        public string ScriptName { get; set; } = "";

        /// <summary>
        /// Тип операции - дополнительный классификатор (например: операции блока безопасности, отчетов и пр.)
        /// </summary>
        public virtual ScriptCommandType? ScriptCommandType { get; set; }
        public int? ScriptCommandTypeId { get; set; }
        
        /// <summary>
        /// Тип (назначение) операции: команда или источник данных 
        /// </summary>
        public ECommandType CommandType
        {
            get => _commandType;
            set
            {
                _commandType = value;
                if (_commandType == ECommandType.DataSource)
                {
                    CodeType = ECodeType.Class;
                    ThreadType = EThreadType.Async;
                }
            }
        } 
        private ECommandType _commandType = ECommandType.Command;

        /// <summary>
        /// Полное наименование типа, к которому может применяться операция.
        /// Может отсутствовать. ВНИМАНИЕ! При его установке автоматически установится (перезапишется) <see cref="EntityType"/>
        /// </summary>
        public string EntityTypeName
        {
            get => _entityTypeName;
            set 
            {
                _entityTypeName = value;
                _entityType = null;
                _entityType = InitEntityType();
            } 
        }
        private string _entityTypeName = "";

        /// <summary>
        /// Тип, к которому применима операция.
        /// ВНИМАНИЕ! Если тип не равен null - при его установке автоматически установится (перезапишется) <see cref="EntityTypeName"/>
        /// </summary>
        public Type? EntityType
        {
            get => InitEntityType();
            set
            {
                _entityType = value;
                if (_entityType != null)
                {
                    _entityTypeName = EntityMetadata.BiuldExtTypeName(_entityType);
                }
            }
        }
        private Type? _entityType;
        
        /// <summary>
        /// Тип кода: простой метод или готовый к компиляции класс
        /// </summary>
        public ECodeType CodeType { get; set; } = ECodeType.Class;
        
        /// <summary>
        /// Если тип кода - простой метод, здесь указывается какой это будет метод: синхронный или асинхронный.
        /// </summary>
        public EThreadType ThreadType { get; set; } = EThreadType.Sync;
        
        /// <summary>
        /// Исполняемый код
        /// </summary>
        public string ScriptCode { get; set; } = "";
        
        /// <summary>
        /// Список параметров
        /// </summary>
        public string ParamListJson { get; set; } = "";

        /// <summary>
        /// Дополнительный тег, который может устанавливаться и использоваться системой
        /// для фильтрации и настройки систем безопасности
        /// </summary>
        public string SystemTag { get; set; } = string.Empty;
        
        /// <summary>
        /// Дополнительный тег, который может устанавливаться и использоваться пользователями
        /// для поиска и фильтрации документов
        /// </summary>
        public string UserTag { get; set; } = string.Empty;

        /// <summary>
        /// Выражение для запуска команды по расписанию в формате cron
        /// </summary>
        public string CronExpression { get; set; } = "";
        
        /// <summary>
        /// Признак того, что необходимо активировать запуск по расписанию для данной команды при запуске приложения.
        /// </summary>
        public bool ScheduleEnabled { get; set; }
        
        /// <summary>
        /// Примечание
        /// </summary>
        public string Comment { get; set; } = "";

        public List<ScriptCommandGroupLink> ScriptCommandGroups { get; set; } = [];

        //public string ScriptCommandGroupNames => ScriptCommandGroups.Select(x => x.ScriptCommandGroup?.Name).Aggregate((first, second) => $"{first}, {second}");
        public string ScriptCommandGroupNames
        {
            get
            {
                string[] groupNames = [.. ScriptCommandGroups
                .Select(x => x.ScriptCommandGroup != null ? x.ScriptCommandGroup.Name : string.Empty)];
                if (groupNames == null || groupNames.Length == 0)
                    return string.Empty;

                return groupNames.Aggregate((first, second) => $"{first}, {second}");
            }
        }

        public List<Field<ScriptCommand>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Скрипт-операция";
            public static readonly Field<ScriptCommand> ScriptName = new()
            {
                Name = nameof(ScriptName), 
                StringGet = x => x.ScriptName, 
                StringSet = (x, val) => x.ScriptName = val ?? "", 
                Required = true, 
                MaxLength = 1024, 
                HumanName = "Имя скрипта"
            };
            
            public static readonly RefField<ScriptCommand, ScriptCommandType> ScriptCommandType = new()
            {
                Name = nameof(FileDocumentType),
                RefGetter = x => x.ScriptCommandType,
                RefSetter = (x, val) => x.ScriptCommandType = val,
                Required = false,
                HumanName = "Тип скрипт-операции",
                HelperText = "Дополнительный классификатор принадлежности операции (по прикладной области)"
            };

            public static readonly Field<ScriptCommand> CommandType = new()
            {
                Name = nameof(CommandType),
                IntGet = x => (int)x.CommandType,
                IntSet = (x, val) => x.CommandType = (val == null) ? ECommandType.Command : (ECommandType)val,
                IntValues = ESelectListItems.CommandTypes,
                Required = true,
                HumanName = "Тип операции",
                HelperText = "Является операция командой или источником данных для отчетов"
            };
            public static readonly Field<ScriptCommand> EntityTypeName = new()
            {
                Name = nameof(EntityTypeName), 
                StringGet = x => x.EntityTypeName, 
                StringSet = (x, val) => x.EntityTypeName = val ?? "", 
                Required = false, 
                MaxLength = 255, 
                HumanName = "Тип объекта",
                HelperText = "Тип объекта, к которому привязывается операция."
            };
            public static readonly Field<ScriptCommand> CodeType = new()
            {
                Name = nameof(CodeType),
                IntGet = x => (int)x.CodeType,
                IntSet = (x, val) => x.CodeType = (val == null) ? ECodeType.SimpleMethod : (ECodeType)val,
                IntValues = ESelectListItems.CodeTypes,
                Required = true,
                HumanName = "Тип кода",
                HelperText = "Исходный код - простая функция или готовый класс"
            };
            public static readonly Field<ScriptCommand> ThreadType = new()
            {
                Name = nameof(ThreadType),
                IntGet = x => (int)x.ThreadType,
                IntSet = (x, val) => x.ThreadType = (val == null) ? EThreadType.Sync : (EThreadType)val,
                IntValues = ESelectListItems.ThreadTypes,
                Required = true,
                HumanName = "Тип выполнения",
                HelperText = "Для простой функции нужно заранее задать синхронность"
            };
            public static readonly Field<ScriptCommand> ScriptCode = new()
            {
                Name = nameof(ScriptCode), 
                StringGet = x => x.ScriptCode, 
                StringSet = (x, val) => x.ScriptCode = val ?? "", 
                Required = false, 
                HumanName = "Код c#"
            };
            public static readonly Field<ScriptCommand> ParamListJson = new()
            {
                Name = nameof(ParamListJson),
                StringGet = x => x.ParamListJson, 
                StringSet = (x, val) => x.ParamListJson = val ?? "",
                Required = false, 
                MaxLength = 2048, 
                HumanName = "Список параметров"
            };
            public static readonly Field<ScriptCommand> EntityType = new()
            {
                Name = nameof(EntityType), 
                StringGet = x => (x.EntityType != null) ? x.EntityType.Name : "", 
                Required = false, 
                HumanName = "Тип объекта (Type)",
                IsPersistent = false
            };
            public static readonly Field<ScriptCommand> SystemTag = new()
            {
                Name = nameof(SystemTag), 
                StringGet = x => x.SystemTag, 
                StringSet = (x, val) => x.SystemTag = val ?? "", 
                Required = false, MaxLength = 512, 
                HumanName = "Системный тег"
            };
            public static readonly Field<ScriptCommand> UserTag = new()
            {
                Name = nameof(UserTag), 
                StringGet = x => x.UserTag, 
                StringSet = (x, val) => x.UserTag = val ?? "", 
                Required = false, MaxLength = 512, 
                HumanName = "Пользовательский тег"
            };

            public static readonly Field<ScriptCommand> CronExpression = new()
            {
                Name = nameof(CronExpression), 
                StringGet = x => x.CronExpression, 
                StringSet = (x, val) => x.CronExpression = val ?? "", 
                Required = false, 
                MaxLength = 64, 
                HumanName = "Выражения для cron",
                HelperText = "Расписание для запуска команды в формате cron"
            };

            public static readonly Field<ScriptCommand> ScheduleEnabled = new()
            {
                Name = nameof(ScheduleEnabled), 
                BoolGet = x => x.ScheduleEnabled, 
                BoolSet = (x, val) => x.ScheduleEnabled = val ?? false, 
                Required = true, 
                HumanName = "Расписание активно"
            };

            public static readonly Field<ScriptCommand> Comment = new()
            {
                Name = nameof(Comment), 
                StringGet = x => x.Comment, 
                StringSet = (x, val) => x.Comment = val ?? "", 
                Required = false, 
                MaxLength = 2048, 
                HumanName = "Комментарий"
            };

            public static readonly Field<ScriptCommand> ScriptCommandGroupNames = new()
            {
                Name = nameof(ScriptCommandGroupNames), 
                StringGet = x => x.ScriptCommandGroupNames, 
                Required = false, 
                HumanName = "Группы",
                IsPersistent = false
            };


            public static readonly List<Field<ScriptCommand>> Fields = [ScriptName, ScriptCommandType, 
                CommandType, EntityTypeName, CodeType, ThreadType, ScriptCode, 
                ParamListJson, EntityType, SystemTag, UserTag, ScheduleEnabled, CronExpression, Comment];

            public static string BuildScriptClassName(ScriptCommand scriptCommand)
            {
                return $"ScriptCommand_{scriptCommand.Id}";
            }
        }
        
        /// <summary>
        /// Получение объекта Type по имени типа.
        /// Если имя типа некорректно, если тип не удалось создать - будет Exception.
        /// <see cref="EntityMetadata"/> не используется, поскольку там только короткие имена типов,
        /// а сюда могут и будут приходить полные имена, с namespace и assembly 
        /// </summary>
        /// <returns></returns>
        private Type? InitEntityType()
        {
            if (_entityType != null)
            {
                return _entityType;
            }
        
            if (EntityTypeName == "")
            {
                _entityType = null;
                return _entityType;
            }
        
            _entityType = Type.GetType(EntityTypeName); 
        
            return _entityType;
        }
        
    }
}
