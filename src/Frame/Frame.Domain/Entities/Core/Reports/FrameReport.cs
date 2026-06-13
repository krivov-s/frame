using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Domain.Entities.Core.Queries;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Params;

namespace Frame.Domain.Entities.Core.Reports
{
    public class FrameReport : BaseEntity, IBaseGenericEntity<FrameReport>
    {
        public override string Description => Name;

        #region =================== Поля модели ===================
        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; } = "";

        public string Descr { get; set; } = "";
        
        /// <summary>
        /// Локальный список параметров, который требуется для подготовки отчета.
        /// При наличии списка параметров в <see cref="SavedQuery"/> они будут объединены в общий.
        /// Объединенный список параметров будет доступен и отчету, и запросу.
        /// </summary>
        public string ParamListJson { get; set; } = "";
        public FrameReportType? FrameReportType { get; set; }
        public int? FrameReportTypeId { get; set; }
        public string EntityTypeName { get; set; } = "";
        
        /// <summary>
        /// Ссылка на образец отчета
        /// </summary>
        public FileDocument? ReportTemplate { get; set; }
        public int? ReportTemplateId { get; set; }
        
        /// <summary>
        /// Источник данных - сохраненный запрос
        /// </summary>
        public SavedQuery? SavedQuery { get; set; }
        public int? SavedQueryId { get; set; }
        
        /// <summary>
        /// Источник данных - скрипт-операция
        /// </summary>
        public ScriptCommand? ScriptCommand { get; set; }
        public int? ScriptCommandId { get; set; }

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

        public List<FrameReportGroupLink> FrameReportGroups { get; set; } = [];

        //вычислимый атрибут названий групп
        public string FrameReportGroupNames
        {
            get
            {
                string[] groupNames = [.. FrameReportGroups
                .Select(x => x.FrameReportGroup != null ? x.FrameReportGroup.Name : string.Empty)];
                if (groupNames == null || groupNames.Length == 0)
                    return string.Empty;

                return groupNames.Aggregate((first, second) => $"{first}, {second}");
            }
        }

        #endregion

        #region =================== Метаданные ===================
        public List<Field<FrameReport>> GetFields() { return Meta.Fields; }

        public static class Meta 
        {
            public static readonly string HumanName = "Отчёт";
            public static readonly Field<FrameReport> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, HumanName = "Название отчёта" };
            public static readonly Field<FrameReport> Descr = new() { Name = nameof(Descr), StringGet = x => x.Descr, StringSet = (x, val) => x.Descr = val ?? "", Required = false, HumanName = "Описание" };
            public static readonly Field<FrameReport> ParamListJson = new() { Name = nameof(ParamListJson), StringGet = x => x.ParamListJson, StringSet = (x, val) => x.ParamListJson = val ?? "", Required = false, HumanName = "Список параметров" };
            public static readonly Field<FrameReport> EntityTypeName = new() { Name = nameof(EntityTypeName), StringGet = x => x.EntityTypeName, StringSet = (x, val) => x.EntityTypeName = val ?? "", Required = false, HumanName = "Название типа сущности" };
            public static readonly Field<FrameReport> SystemTag = new() { Name = nameof(SystemTag), StringGet = x => x.SystemTag, StringSet = (x, val) => x.SystemTag = val ?? "", Required = false, HumanName = "Системный тег" };
            public static readonly Field<FrameReport> UserTag = new() { Name = nameof(UserTag), StringGet = x => x.UserTag, StringSet = (x, val) => x.UserTag = val ?? "", Required = false, HumanName = "Пользовательский тег" };
            public static readonly Field<FrameReport> FrameReportGroupNames = new()
            {
                Name = nameof(FrameReportGroupNames), 
                StringGet = x => x.FrameReportGroupNames, 
                Required = false, 
                HumanName = "Группы",
                IsPersistent = false
            };

            public static readonly RefField<FrameReport, FrameReportType> FrameReportType = new()
            {
                Name = nameof(FrameReportType),
                RefGetter = x => x.FrameReportType,
                RefIdGetter = x => x.FrameReportTypeId,
                RefSetter = (x, val) => x.FrameReportType = val,
                RefIdSetter = (x, val) => x.FrameReportTypeId = val,
                Required = false,
                HumanName = "Тип отчёта"
            };
            public static readonly RefField<FrameReport, SavedQuery> SavedQuery = new()
            {
                Name = nameof(SavedQuery),
                RefGetter = x => x.SavedQuery,
                RefSetter = (x, val) => x.SavedQuery = val,
                RefIdGetter = x => x.SavedQueryId,
                RefIdSetter = (x, val) => x.SavedQueryId = val,
                Required = false,
                HumanName = "Запрос",
                HelperText = "Источник данных - сохраненный запрос"
            };
            public static readonly RefField<FrameReport, ScriptCommand> ScriptCommand = new()
            {
                Name = nameof(ScriptCommand),
                RefGetter = x => x.ScriptCommand,
                RefSetter = (x, val) => x.ScriptCommand = val,
                RefIdGetter = x => x.ScriptCommandId,
                RefIdSetter = (x, val) => x.ScriptCommandId = val,
                // Добавляем условие, что источником данных может быть только скрипт-операция с типом DataSource
                // Это будет использоваться во всех RefEdit-ах.
                ConfigureQuerySpecification = qs => qs.Where(sc => sc.CommandType == ECommandType.DataSource), 
                Required = false,
                HumanName = "Скрипт-операция",
                HelperText = $"Источник данных - скрипт-операция с типом {nameof(ECommandType.DataSource)}" 
            };
            public static readonly RefField<FrameReport, FileDocument> ReportTemplate = new()
            {
                Name = nameof(ReportTemplate),
                RefGetter = x => x.ReportTemplate,
                RefSetter = (x, val) => x.ReportTemplate = val,
                RefIdGetter = x => x.ReportTemplateId,
                RefIdSetter = (x, val) => x.ReportTemplateId = val,
                Required = false,
                HumanName = "Шаблон отчета",
                HelperText = "Файл-документ, содержащий в себе шаблон отчета"
            };
            public static readonly List<Field<FrameReport>> Fields = [Name, Descr, ParamListJson, EntityTypeName, 
                SystemTag, UserTag, FrameReportType, SavedQuery, ScriptCommand, ReportTemplate ];
        }

        #endregion

        /// <summary>
        /// Построение результирующего списка параметров как суммы (объединения) параметров
        /// отчета <see cref="ParamListJson"/> и запроса <see cref="SavedQuery.ParamListJson"/>
        /// или автооперации - источника данных <see cref="ScriptCommand.ParamListJson"/>.
        /// 
        /// </summary>
        /// <returns>Объединенный список параметров</returns>
        public ParamList BuildParamList()
        {
            ParamList pl1 = new ParamList();
            ParamList pl2 = new ParamList();
            if (ParamListJson.Length > 0)
            {
                pl1.FromJson(ParamListJson);
            }
            if(SavedQuery != null && SavedQuery.ParamListJson.Length > 0)
            {
                pl2.FromJson(SavedQuery.ParamListJson);
            }
            else if(ScriptCommand != null && ScriptCommand.ParamListJson.Length > 0)
            {
                pl2.FromJson(ScriptCommand.ParamListJson);
            } 
            return pl1 + pl2;
        }
    }
}
