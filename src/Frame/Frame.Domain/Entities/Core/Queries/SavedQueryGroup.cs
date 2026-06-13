namespace Frame.Domain.Entities.Core.Queries
{
    public class SavedQueryGroup : BaseEntity, IBaseGenericEntity<SavedQueryGroup>
    {
        public override string Description => Name;


        public string Name { get; set; } = string.Empty;
        public string Descr { get; set; } = "";
        public List<SavedQueryGroupLink> SavedQueries { get; set; } = [];

        List<Field<SavedQueryGroup>> IBaseGenericEntity<SavedQueryGroup>.GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Группа сохранённых отчётов";
            public static readonly Field<SavedQueryGroup> Name = new()
            {
                Name = nameof(Name),
                StringGet = x => x.Name,
                StringSet = (x, val) => x.Name = val ?? "",
                Required = true,
                MaxLength = 80,
                HumanName = "Название группы сохранённых отчётов"
            };
            public static readonly Field<SavedQueryGroup> Descr = new()
            {
                Name = nameof(Descr),
                StringGet = x => x.Descr,
                StringSet = (x, val) => x.Descr = val ?? "",
                Required = false,
                MaxLength = 2048,
                HumanName = "Описание"
            };

            public static readonly List<Field<SavedQueryGroup>> Fields = [Name, Descr];
        }
    }
}
