
namespace Frame.Domain.Entities.Core.FileDocuments
{
    public class FileDocument : BaseEntity, IBaseGenericEntity<FileDocument>
    {
        public override string Description
        {
            get
            {
                string sName;
                if(FileDocumentType != null)
                {
                    sName = $"{DocumentName} [{FileDocumentType.Name}]";
                }
                else
                {
                    sName = $"{DocumentName}";
                }
                if(sName.Length == 0)
                {
                    sName = OwnerKey + " => " + FileKey;
                }
                return sName;
            }
        }
        /// <summary>
        /// Признак актуальности документа. Если не установлен - считается архивным.
        /// </summary>
        public bool IsActual { get; set; } = true;
        
        /// <summary>
        /// "Человеческое" наименование документа.
        /// </summary>
        public string DocumentName { get; set; } = string.Empty;
        
        /// <summary>
        /// Ключ, по которому будет осуществляться поиск файла в хранилище документов.
        /// <para>Для FileStorage - это путь к файлу относительно базовой директории хранилища в формате "path/to/file.pdf".</para>
        /// <para>Для S3 Storage - это полный путь к файлу внутри бакета в формате "path/to/file.pdf".</para>
        /// </summary>
        public string? FileKey { get; set; }
        
        /// <summary>
        /// Ключ объекта - владельца файла. 
        /// <para>
        /// Предполагается что это свойство BaseEntity.Key объекта-владельца. 
        /// По этому ключу может осуществляться выборка и фильтрация файл-документов.
        /// </para>
        /// </summary>
        public string OwnerKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Имя файла по-умолчанию при загрузке пользователю. Опционально.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Описание, доп. информация к документу.
        /// </summary>
        public string Comment { get; set; } = string.Empty;
        
        /// <summary>
        /// Тип документа - дополнительный классификатор (например: сканы договоров, планировочные решения, визуализации, и т.п.)
        /// </summary>
        public virtual FileDocumentType? FileDocumentType { get; set; }
        public int? FileDocumentTypeId { get; set; }

        /// <summary>
        /// Дополнительный тег к документу, который может устанавливаться и использоваться системой
        /// для фильтрации и настройки систем безопасности
        /// </summary>
        public string SystemTag { get; set; } = string.Empty;
        
        /// <summary>
        /// Дополнительный тег к документу, который может устанавливаться и использоваться пользователями
        /// для поиска и фильтрации документов
        /// </summary>
        public string UserTag { get; set; } = string.Empty;

        public bool IsImage()
        {
            if (FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return true;
            else return false;
        }

        public List<Field<FileDocument>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Файл-документ";
            public static readonly Field<FileDocument> IsActual = new() { Name = nameof(IsActual), BoolGet = x => x.IsActual, BoolSet = (x, val) => x.IsActual = val ?? false, Required = true, HumanName = "Признак актуальности документа" };
            public static readonly Field<FileDocument> DocumentName = new() { Name = nameof(DocumentName), StringGet = x => x.DocumentName, StringSet = (x, val) => x.DocumentName = val ?? "", Required = true, MaxLength = 1024, HumanName = "Наименование документа" };
            public static readonly Field<FileDocument> FileKey = new() { Name = nameof(FileKey), StringGet = x => x.FileKey, StringSet = (x, val) => x.FileKey = val ?? "", Required = false, MaxLength = 1024, HumanName = "Ключ файл-документа" };
            public static readonly Field<FileDocument> OwnerKey = new() { Name = nameof(OwnerKey), StringGet = x => x.OwnerKey, StringSet = (x, val) => x.OwnerKey = val ?? "", Required = false, MaxLength = 1024, HumanName = "Ключ владельца" };
            public static readonly Field<FileDocument> FileName = new() { Name = nameof(FileName), StringGet = x => x.FileName, StringSet = (x, val) => x.FileName = val ?? "", Required = false, MaxLength = 255, HumanName = "Имя файла" };
            public static readonly Field<FileDocument> SystemTag = new() { Name = nameof(SystemTag), StringGet = x => x.SystemTag, StringSet = (x, val) => x.SystemTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Системный тег" };
            public static readonly Field<FileDocument> UserTag = new() { Name = nameof(UserTag), StringGet = x => x.UserTag, StringSet = (x, val) => x.UserTag = val ?? "", Required = false, MaxLength = 512, HumanName = "Пользовательский тег" };
            public static readonly Field<FileDocument> Comment = new() { Name = nameof(Comment), StringGet = x => x.Comment, StringSet = (x, val) => x.Comment = val ?? "", Required = false, MaxLength = 4096, HumanName = "Комментарий" };
            public static readonly RefField<FileDocument, FileDocumentType> FileDocumentType = new()
            {
                Name = nameof(FileDocumentType),
                RefGetter = x => x.FileDocumentType,
                RefIdGetter = x => x.FileDocumentTypeId,
                RefSetter = (x, val) => x.FileDocumentType = val,
                RefIdSetter = (x, val) => x.FileDocumentTypeId = val,
                Required = false,
                HumanName = "Тип файл-документа"
            };

            public static readonly List<Field<FileDocument>> Fields = [IsActual, FileKey, DocumentName, FileDocumentType, OwnerKey, FileName, SystemTag, UserTag, Comment];
        }
    }
}
