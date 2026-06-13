namespace Frame.Domain.Entities.Core.Queries
{
    public class SavedQueryGroupLink : BaseEntity, IBaseGenericEntity<SavedQueryGroupLink>
    {
        public override string Description => "Связка Группа сохранённых запросов - Роль";

        virtual public SavedQueryGroup? SavedQueryGroup { get; set; }
        public int SavedQueryGroupId { get; set; }
        virtual public SavedQuery? SavedQuery { get; set; }
        public int SavedQueryId { get; set; }

        public List<Field<SavedQueryGroupLink>> GetFields() { return Meta.Fields; }

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Связка Сохранённы-Пользователь";
            public static readonly RefField<SavedQueryGroupLink, SavedQueryGroup> SavedQueryGroup = new()
            {
                Name = nameof(SavedQueryGroup),
                RefGetter = x => x.SavedQueryGroup,
                RefIdGetter = x => x.SavedQueryGroupId,
                RefSetter = (x, val) => x.SavedQueryGroup = val,
                RefIdSetter = (x, val) => x.SavedQueryGroupId = val ?? 0,
                Required = true,
                HumanName = "Группа сохранённых запросов"
            };
            public static readonly RefField<SavedQueryGroupLink, SavedQuery> SavedQuery = new()
            {
                Name = nameof(SavedQuery),
                RefGetter = x => x.SavedQuery,
                RefIdGetter = x => x.SavedQueryId,
                RefSetter = (x, val) => x.SavedQuery = val,
                RefIdSetter = (x, val) => x.SavedQueryId = val ?? 0,
                Required = true,
                HumanName = "Сохранённый запрос"
            };

            public static readonly List<Field<SavedQueryGroupLink>> Fields = [SavedQueryGroup, SavedQuery];
        }
        #endregion
    }
}