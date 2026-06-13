using Frame.Domain.Entities.Core.FileDocuments;
using Frame.Domain.Entities.Core.Params;

namespace Frame.Domain.Entities.Core
{
    /// <summary>
    /// Настройки прикладной части ядра системы
    /// </summary>
    public class FrameSettings : BaseEntity, IBaseGenericEntity<FrameSettings>
    {
        public virtual UserProfile? SystemUserProfile { get; set; }
        public int SystemUserProfileId { get; set; }
        public string ScriptCommandClassSyncCodeSample { get; set; } = "";
        public string ScriptCommandClassAsyncCodeSample { get; set; } = "";
        public string ScriptCommandMethodSyncCodeSample { get; set; } = "";
        public string ScriptCommandMethodAsyncCodeSample { get; set; } = "";
        public string ScriptCommandDataSourceCodeSample { get; set; } = "";
        
        /// <summary>
        /// Тип файл-документа, который является отчетом. Подставляется по-умолчанию в каждый файл отчета.
        /// </summary>
        public FileDocumentType? ReportFileDocumentType { get; set; }
        public int? ReportFileDocumentTypeId { get; set; }
        
        /// <summary>
        /// Минимальный набор using для включения в любой скрипт
        /// </summary>
        public string DefaultUsing { get; set; } = "";

        public List<Field<FrameSettings>> GetFields()
        {
            return Meta.Fields;
        }

        public static class Meta
        {
            public static readonly string HumanName = "Настройки ядра системы";

            public static readonly RefField<FrameSettings, UserProfile> SystemUserProfile = new()
            {
                Name = nameof(SystemUserProfile),
                RefGetter = x => x.SystemUserProfile,
                RefIdGetter = x => x.SystemUserProfileId,
                RefSetter = (x, val) => x.SystemUserProfile = val,
                RefIdSetter = (x, val) => x.SystemUserProfileId = val ?? 0,
                Required = true,
                HumanName = "Профиль системного пользователя"
            };

            public static readonly Field<FrameSettings> ScriptCommandClassSyncCodeSample = new()
            {
                Name = nameof(ScriptCommandClassSyncCodeSample),
                StringGet = x => x.ScriptCommandClassSyncCodeSample,
                StringSet = (x, val) => x.ScriptCommandClassSyncCodeSample = val ?? "",
                Required = false,
                HumanName = "Образец для IScriptCommandSync",
                HelperText = "Образец кода для синхронной реализации интерфейса IScriptCommandSync"
            };

            public static readonly Field<FrameSettings> ScriptCommandClassAsyncCodeSample = new()
            {
                Name = nameof(ScriptCommandClassAsyncCodeSample),
                StringGet = x => x.ScriptCommandClassAsyncCodeSample,
                StringSet = (x, val) => x.ScriptCommandClassAsyncCodeSample = val ?? "",
                Required = false,
                HumanName = "Образец для IScriptCommandAsync",
                HelperText = "Образец кода для асинхронной реализации интерфейса IScriptCommandAsync"
            };

            public static readonly Field<FrameSettings> ScriptCommandMethodSyncCodeSample = new()
            {
                Name = nameof(ScriptCommandMethodSyncCodeSample),
                StringGet = x => x.ScriptCommandMethodSyncCodeSample,
                StringSet = (x, val) => x.ScriptCommandMethodSyncCodeSample = val ?? "",
                Required = false,
                HumanName = "Образец для простого синхронного метода",
                HelperText = "Образец кода для синхронной реализации простого метода автооперации"
            };

            public static readonly Field<FrameSettings> ScriptCommandMethodAsyncCodeSample = new()
            {
                Name = nameof(ScriptCommandMethodAsyncCodeSample),
                StringGet = x => x.ScriptCommandMethodAsyncCodeSample,
                StringSet = (x, val) => x.ScriptCommandMethodAsyncCodeSample = val ?? "",
                Required = false,
                HumanName = "Образец для простого асинхронного метода",
                HelperText = "Образец кода для асинхронной реализации простого метода автооперации"
            };

            public static readonly Field<FrameSettings> ScriptCommandDataSourceCodeSample = new()
            {
                Name = nameof(ScriptCommandDataSourceCodeSample),
                StringGet = x => x.ScriptCommandDataSourceCodeSample,
                StringSet = (x, val) => x.ScriptCommandDataSourceCodeSample = val ?? "",
                Required = false,
                HumanName = "Образец для IScriptCommandDataSource",
                HelperText = "Образец кода для реализации интерфейса IScriptCommandDataSource"
            };
            public static readonly Field<FrameSettings> DefaultUsing = new()
            {
                Name = nameof(DefaultUsing),
                StringGet = x => x.DefaultUsing,
                StringSet = (x, val) => x.DefaultUsing = val ?? "",
                Required = false,
                HumanName = "Базовый блок using",
                HelperText = "Минимальный набор необходимых библиотек (ссылок)"
            };
            public static readonly RefField<FrameSettings, FileDocumentType> ReportFileDocumentType = new()
            {
                Name = nameof(ReportFileDocumentType),
                RefGetter = x => x.ReportFileDocumentType,
                RefIdGetter = x => x.ReportFileDocumentTypeId,
                RefSetter = (x, val) => x.ReportFileDocumentType = val,
                RefIdSetter = (x, val) => x.ReportFileDocumentTypeId = val,
                Required = false,
                HumanName = "Тип файл-документа шаблона отчета",
                HelperText = "Тип файл-документа, который будет автоматически подставляться сохраняемому шаблону отчета"
            };

            public static readonly List<Field<FrameSettings>> Fields =
            [
                SystemUserProfile,
                ScriptCommandClassSyncCodeSample, ScriptCommandClassAsyncCodeSample,
                ScriptCommandMethodSyncCodeSample, ScriptCommandMethodAsyncCodeSample,
                ScriptCommandDataSourceCodeSample, DefaultUsing, ReportFileDocumentType
            ];
        }
    }
}
