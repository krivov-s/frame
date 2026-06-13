
namespace Frame.Domain.Entities.Core.Scripting
{
    public class ScriptCommandType : BaseEntity, IBaseGenericEntity<ScriptCommandType>
    {
        public override string Description => Name;

        public string Name { get; set; } = "";
        public string Descr { get; set; } = "";
        
        public List<Field<ScriptCommandType>> GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Тип скрипт-операции";
            public static readonly Field<ScriptCommandType> Name = new() { Name = nameof(Name), StringGet = x => x.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 255, HumanName = "Наименование" };
            public static readonly Field<ScriptCommandType> Descr = new() { Name = nameof(Descr), StringGet = x => x.Descr, StringSet = (x, val) => x.Descr = val ?? "", Required = false, MaxLength = 2048, HumanName = "Описание" };

            public static readonly List<Field<ScriptCommandType>> Fields = [Name, Descr];
        }
    }
}
