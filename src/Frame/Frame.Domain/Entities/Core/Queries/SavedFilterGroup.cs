namespace Frame.Domain.Entities.Core.Queries
{
    public class SavedFilterGroup : BaseEntity, IBaseGenericEntity<SavedFilterGroup>
    {
        public override string Description => Name;

        public string Name { get; set; } = string.Empty;
        public string Descr { get; set; } = "";
        public List<SavedFilterGroupLink> SavedFilters { get; set; } = [];

        List<Field<SavedFilterGroup>> IBaseGenericEntity<SavedFilterGroup>.GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Группа сохранённых фильтров";
            public static readonly Field<SavedFilterGroup> Name = new()
            {
                Name = nameof(Name),
                StringGet = x => x.Name,
                StringSet = (x, val) => x.Name = val ?? "",
                Required = true,
                MaxLength = 80,
                HumanName = "Название группы сохранённых фильтров"
            };
            public static readonly Field<SavedFilterGroup> Descr = new()
            {
                Name = nameof(Descr),
                StringGet = x => x.Descr,
                StringSet = (x, val) => x.Descr = val ?? "",
                Required = false,
                MaxLength = 2048,
                HumanName = "Описание"
            };

            public static readonly List<Field<SavedFilterGroup>> Fields = [Name, Descr];
        }
    }
}
