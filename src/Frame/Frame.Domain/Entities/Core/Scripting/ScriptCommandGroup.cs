namespace Frame.Domain.Entities.Core.Scripting
{
    public class ScriptCommandGroup : BaseEntity, IBaseGenericEntity<ScriptCommandGroup>
    {
        public override string Description => Name;

        public string Name { get; set; } = string.Empty;
        public string Descr { get; set; } = "";
        public List<ScriptCommandGroupLink> ScriptCommands { get; set; } = [];

        List<Field<ScriptCommandGroup>> IBaseGenericEntity<ScriptCommandGroup>.GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Группа сохранённых скрипт-команд";
            public static readonly Field<ScriptCommandGroup> Name = new()
            {
                Name = nameof(Name),
                StringGet = x => x.Name,
                StringSet = (x, val) => x.Name = val ?? "",
                Required = true,
                MaxLength = 80,
                HumanName = "Название группы сохранённых скрипт-команд"
            };
            public static readonly Field<ScriptCommandGroup> Descr = new()
            {
                Name = nameof(Descr),
                StringGet = x => x.Descr,
                StringSet = (x, val) => x.Descr = val ?? "",
                Required = false,
                MaxLength = 2048,
                HumanName = "Описание"
            };

            public static readonly List<Field<ScriptCommandGroup>> Fields = [Name, Descr];
        }
    }
}
