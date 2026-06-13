namespace Frame.Domain.Entities.Core.Security
{
    public class Division : BaseEntity, IBaseGenericEntity<Division>
    {
        public override string Description => $"{Name} ({SysCode})";

        #region =================== Поля модели =================== 
        /// <summary>
        /// Системный код подразделения
        /// </summary>
        public int SysCode { get; set; }
        /// <summary>
        /// Краткое наименование подразделения
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Примечание
        /// </summary>
        public string Comment { get; set; } = "";

        #endregion

        #region =================== Метаданные =================== 
        public List<Field<Division>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Подразделение";
            public static readonly Field<Division> SysCode = new() { Name = nameof(SysCode), IntGet = x => x.SysCode, IntSet = (x, val) => x.SysCode = val ?? 0, Required = true, HumanName = "Системный код" };
            public static readonly Field<Division> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 255, HumanName = "Наименование" };
            public static readonly Field<Division> Comment = new() { Name = nameof(Comment), StringGet = x => x.Comment, StringSet = (x, val) => x.Comment = val ?? "", Required = false, MaxLength = 2048, HumanName = "Комментарий" };

            public static readonly List<Field<Division>> Fields = [SysCode, Name, Comment];

            #endregion
        }
    }
}
