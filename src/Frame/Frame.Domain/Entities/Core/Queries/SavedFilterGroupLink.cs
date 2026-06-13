namespace Frame.Domain.Entities.Core.Queries
{
    public class SavedFilterGroupLink : BaseEntity, IBaseGenericEntity<SavedFilterGroupLink>
    {
        public override string Description => "Связка Группа сохранённых фильтров - Сохранённый фильтр";

        virtual public SavedFilterGroup? SavedFilterGroup { get; set; }
        public int SavedFilterGroupId { get; set; }
        virtual public SavedFilter? SavedFilter { get; set; }
        public int SavedFilterId { get; set; }

        public List<Field<SavedFilterGroupLink>> GetFields() { return Meta.Fields; }

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Связка Группа сохранённых фильтров - Сохранённый фильтр";
            public static readonly RefField<SavedFilterGroupLink, SavedFilterGroup> SavedFilterGroup = new()
            {
                Name = nameof(SavedFilterGroup),
                RefGetter = x => x.SavedFilterGroup,
                RefIdGetter = x => x.SavedFilterGroupId,
                RefSetter = (x, val) => x.SavedFilterGroup = val,
                RefIdSetter = (x, val) => x.SavedFilterGroupId = val ?? 0,
                Required = true,
                HumanName = "Группа сохранённых фильтров"
            };
            public static readonly RefField<SavedFilterGroupLink, SavedFilter> SavedFilter = new()
            {
                Name = nameof(SavedFilter),
                RefGetter = x => x.SavedFilter,
                RefIdGetter = x => x.SavedFilterId,
                RefSetter = (x, val) => x.SavedFilter = val,
                RefIdSetter = (x, val) => x.SavedFilterId = val ?? 0,
                Required = true,
                HumanName = "Сохранённый фильтр"
            };

            public static readonly List<Field<SavedFilterGroupLink>> Fields = [SavedFilterGroup, SavedFilter];
        }
        #endregion
    }
}
