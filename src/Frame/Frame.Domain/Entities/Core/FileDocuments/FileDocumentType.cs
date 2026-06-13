
namespace Frame.Domain.Entities.Core.FileDocuments
{
    public class FileDocumentType : BaseEntity, IBaseGenericEntity<FileDocumentType>
    {
        public override string Description => Name;

        public string Name { get; set; } = "";
        public string Descr { get; set; } = "";
        
        /// <summary>
        /// Ссылка на путь к папке относительно корня файла-сервера.
        /// При первом создании (сохранении, загрузки документа) все файлы данного типа будут укладываться в эту папку.
        /// При изменении типа файл в другую папку не переносится, ключ не меняется.
        /// </summary>
        public string PathToFolder { get; set; } = "";

        public List<Field<FileDocumentType>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Тип файл-документа";
            public static readonly Field<FileDocumentType> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 255, HumanName = "Наименование" };
            public static readonly Field<FileDocumentType> Descr = new() { Name = nameof(Descr), StringGet = x => x.Descr, StringSet = (x, val) => x.Descr = val ?? "", Required = false, MaxLength = 2048, HumanName = "Описание" };
            public static readonly Field<FileDocumentType> PathToFolder = new() { Name = nameof(PathToFolder), StringGet = x => x.PathToFolder, StringSet = (x, val) => x.PathToFolder = val ?? "", Required = false, MaxLength = 2048, HumanName = "Путь к папке" };

            public static readonly List<Field<FileDocumentType>> Fields = [Name, Descr, PathToFolder];
        }
    }
}
