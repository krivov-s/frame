using System.Reflection;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Metadata;
using Frame.Shared;

namespace Frame.App.Security
{
    /// <summary>
    /// Вспомогательный класс - профиль безопасности для конкретного типа конкретной роли. 
    /// <para/>
    /// Используется в карте прав доступа в объекте сессии пользователя <see cref="UserSession"/>
    /// </summary>
    public class SecurityProfile
    {
        public string ReadQueryFilter { get; set; } = "";
        public string CreateTLS { get; set; } = "";
        public string CreateOLS { get; set; } = "";
        public string ViewTLS { get; set; } = "";
        public string ViewOLS { get; set; } = "";
        public string ModifyTLS { get; set; } = "";
        public string ModifyOLS { get; set; } = "";
        public string CopyTLS { get; set; } = "";
        public string CopyOLS { get; set; } = "";
        public string DeleteTLS { get; set; } = "";
        public string DeleteOLS { get; set; } = "";
        public string AuditTLS { get; set; } = "";
        public string AuditOLS { get; set; } = "";
        public string Print { get; set; } = "";
        public string PrintPreview { get; set; } = "";
        public string SaveAsTemplate { get; set; } = "";
        public string CreateFromTemplate { get; set; } = "";
        public string Export { get; set; } = "";


        /// <summary>
        /// Список атрибутов, которые можно читать. Если не задано - можно все.
        /// </summary>
        public List<string> ReadAttrsList { get; private set; } = [];
        
        /// <summary>
        /// Список атрибутов, которые можно изменять. Если не задано - можно все.
        /// </summary>
        public List<string> ModifyAttrsList { get; private set; } = [];
        
        private EntityMetaInfo? _entityMetaInfo = null;
        private List<string> _attrNames = [];
        
        public void GrantAll()
        {
            CreateTLS = "True";
            CreateOLS = "True";
            ReadQueryFilter = "True";
            ViewTLS = "True";
            ViewOLS = "True";
            ModifyTLS = "True";
            ModifyOLS = "True";
            CopyTLS = "True";
            CopyOLS = "True";
            DeleteTLS = "True";
            DeleteOLS = "True";
            AuditTLS = "True";
            AuditOLS = "True";
            Print = "True";
            PrintPreview = "True";
            SaveAsTemplate = "True";
            CreateFromTemplate = "True";
            Export = "True";
        }

        public void GrantAllToEmpty()
        {
            CreateTLS = (CreateTLS.Length == 0) ? "True" : CreateTLS;
            CreateOLS = (CreateTLS.Length == 0) ? "True" : CreateOLS;
            ReadQueryFilter = (ReadQueryFilter.Length == 0) ? "True" : ReadQueryFilter;
            ViewTLS = (ViewTLS.Length == 0) ? "True" : ViewTLS;
            ViewOLS = (ViewOLS.Length == 0) ? "True" : ViewOLS;
            ModifyTLS = (ModifyTLS.Length == 0) ? "True" : ModifyTLS;
            ModifyOLS = (ModifyOLS.Length == 0) ? "True" : ModifyOLS;
            CopyTLS = (CopyTLS.Length == 0) ? "True" : CopyTLS;
            CopyOLS = (CopyOLS.Length == 0) ? "True" : CopyOLS;
            DeleteTLS = (DeleteTLS.Length == 0) ? "True" : CopyTLS;
            DeleteOLS = (DeleteOLS.Length == 0) ? "True" : CopyOLS;
            AuditTLS = (AuditTLS.Length == 0) ? "True" : AuditTLS;
            AuditOLS = (AuditOLS.Length == 0) ? "True" : AuditOLS;
            Print = (Print.Length == 0) ? "True" : Print;
            PrintPreview = (PrintPreview.Length == 0) ? "True" : PrintPreview;
            SaveAsTemplate = (SaveAsTemplate.Length == 0) ? "True" : SaveAsTemplate;
            CreateFromTemplate = (CreateFromTemplate.Length == 0) ? "True" : CreateFromTemplate;
            Export = (Export.Length == 0) ? "True" : Export;
            ReadAttrsList = [];
            ModifyAttrsList = [];
        }

        public void AddFilters(TEntityRights entityRights)
        {
            if (_entityMetaInfo == null)
            {
                _entityMetaInfo = EntityMetadata.GetEntityMeta(entityRights.EntityTypeName);
                if (_entityMetaInfo == null)
                {
                    throw new FrameSecurityException($"В списке системных типов (моделей) не удалось найти {entityRights.EntityTypeName}");
                }

                // Забираем в массив все имена публичных свойств, определенных в метаданных нашего класса
                _attrNames = _entityMetaInfo.Fields.Select(f => f.FrameField.Name).ToList();
            }

            if (_entityMetaInfo.DotNetType.Name != entityRights.EntityTypeName)
            {
                throw new FrameSecurityException($"Попытка объединить права доступа для типа {entityRights.EntityTypeName} " +
                                                 $"с типом {_entityMetaInfo.DotNetType.Name}");
            }
            
            CreateTLS = CheckFilter(CreateTLS, entityRights.CreateTLS);
            CreateOLS = CheckFilter(CreateOLS, entityRights.CreateOLS);
            ReadQueryFilter = CheckFilter(ReadQueryFilter, entityRights.ReadQueryFilter);
            ViewTLS = CheckFilter(ViewTLS, entityRights.ViewTLS);
            ViewOLS = CheckFilter(ViewOLS, entityRights.ViewOLS);
            ModifyTLS = CheckFilter(ModifyTLS, entityRights.ModifyTLS);
            ModifyOLS = CheckFilter(ModifyOLS, entityRights.ModifyOLS);
            CopyTLS = CheckFilter(CopyTLS, entityRights.CopyTLS);
            CopyOLS = CheckFilter(CopyOLS, entityRights.CopyOLS);
            DeleteTLS = CheckFilter(DeleteTLS, entityRights.DeleteTLS);
            DeleteOLS = CheckFilter(DeleteOLS, entityRights.DeleteOLS);
            AuditTLS = CheckFilter(AuditTLS, entityRights.AuditTLS);
            AuditOLS = CheckFilter(AuditOLS, entityRights.AuditOLS);
            Print = CheckFilter(Print, entityRights.Print);
            PrintPreview = CheckFilter(PrintPreview, entityRights.PrintPreview);
            SaveAsTemplate = CheckFilter(SaveAsTemplate, entityRights.SaveAsTemplate);
            CreateFromTemplate = CheckFilter(CreateFromTemplate, entityRights.CreateFromTemplate);
            Export = CheckFilter(Export, entityRights.Export);
            
            _addPropLists(entityRights);
        }

        public string GetFilter(EntityOperationType operationType) => operationType switch
        {
            EntityOperationType.CreateTls => CreateTLS.Trim(),
            EntityOperationType.CreateOls => CreateOLS.Trim(),
            EntityOperationType.ViewTls => ViewTLS.Trim(),
            EntityOperationType.ViewOls => ViewOLS.Trim(),
            EntityOperationType.ModifyTls => ModifyTLS.Trim(),
            EntityOperationType.ModifyOls => ModifyOLS.Trim(),
            EntityOperationType.CopyTls => CopyTLS.Trim(),
            EntityOperationType.CopyOls => CopyOLS.Trim(),
            EntityOperationType.DeleteTls => DeleteTLS.Trim(),
            EntityOperationType.DeleteOls => DeleteOLS.Trim(),
            EntityOperationType.AuditTls => AuditTLS.Trim(),
            EntityOperationType.AuditOls => AuditOLS.Trim(),
            EntityOperationType.Print => Print.Trim(),
            EntityOperationType.PrintPreview => PrintPreview.Trim(),
            EntityOperationType.SaveAsTemplate => SaveAsTemplate.Trim(),
            EntityOperationType.CreateFromTemplate => CreateFromTemplate.Trim(),
            EntityOperationType.Export => Export.Trim(),
            _ => ""
        };

        private static string CheckFilter(string srcFilter, string addFilter)
        {
            if (addFilter.Length == 0)
            {
                return srcFilter;
            }
            else
            {
                if(srcFilter.Length == 0)
                {
                    return addFilter;
                }
                else
                {
                    return $"({srcFilter}) || ({addFilter})";
                }
            }
        }


        private void _addPropLists(TEntityRights entityRights)
        {
            _addPropList(ReadAttrsList, entityRights.ReadAttrs, entityRights.CanReadAttrs);
            _addPropList(ModifyAttrsList, entityRights.ModifyAttrs, entityRights.CanModifyAttrs);
        }

        /// <summary>
        /// Добавление (объединение) списка свойств с учетом признака прямой/инверсный список 
        /// </summary>
        /// <param name="attrsList">Список свойств, в который добавляем</param>
        /// <param name="addPropList">Строка со списком свойств для добавления</param>
        /// <param name="isDirectList">Признак: задан прямой или инверсный список</param>
        private void _addPropList(List<string> attrsList, string? addPropList, bool isDirectList)
        {
            if(string.IsNullOrEmpty(addPropList)) return;
            if (_entityMetaInfo == null) throw new FrameException("Ошибка инициализации: отсутствует EntityMetaInfo");

            List<string> newList = [];
            addPropList.Split(',').ToList().ForEach(x =>
            {
                // В карту добавляем только если имя атрибута принадлежит TEntity, совпадает с одним их его атрибутов
                string attr = x.Trim();
                if (_entityMetaInfo.Fields.Any(f => f.FrameField.Name == attr))
                {
                    newList.Add(attr);
                }
            });

            if (isDirectList)
            {
                // Для прямого списка просто проверяем, если свойство отсутствует в конечном - добавляем,
                // т.е. в newList задаются те свойства, которые будут доступны для чтения/редактирования
                attrsList.AddRange(newList.Where(a => !attrsList.Contains(a)));
            }
            else
            {
                // Для инверсного списка добавляем все свойства объекта, которые не входят в newList, 
                // т.е. в newList задаются те свойства, которые НЕ будут доступны для чтения/редактирования
                // (а все остальные - будут доступны)
                attrsList.AddRange(_attrNames.Where(a => 
                    !newList.Contains(a) && !attrsList.Contains(a)));
                
                // Отработаем ситуацию, когда в addPropList все свойства объекта, тогда в результате вычитания
                // множеств attrsList будет пустым, и это значит что все поля доступны,
                // а на самом деле должно быть наоборот. В этом случае добавляем dummy field
                if (newList.Count == _attrNames.Count && attrsList.Count == 0)
                {
                    attrsList.Add("DUMMY_FIELD");
                }
            }
        }
    }
    
    public enum EntityOperationType
    {
        CreateTls,
        CreateOls,
        ViewTls,
        ViewOls,
        ModifyTls,
        ModifyOls,
        CopyTls,
        CopyOls,
        DeleteTls,
        DeleteOls,
        AuditTls,
        AuditOls,
        Print,
        PrintPreview,
        SaveAsTemplate,
        CreateFromTemplate,
        Export,
        ReadAttrs,
        ModifyAttrs
    }
}
