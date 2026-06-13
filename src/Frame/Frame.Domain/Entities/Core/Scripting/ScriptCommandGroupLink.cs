namespace Frame.Domain.Entities.Core.Scripting
{
    public class ScriptCommandGroupLink : BaseEntity, IBaseGenericEntity<ScriptCommandGroupLink>
    {
        public override string Description => "Связка Группа скрипт-операций";

        virtual public ScriptCommandGroup? ScriptCommandGroup { get; set; }
        public int ScriptCommandGroupId { get; set; }
        virtual public ScriptCommand? ScriptCommand { get; set; }
        public int ScriptCommandId { get; set; }

        public List<Field<ScriptCommandGroupLink>> GetFields() { return Meta.Fields; }

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Связка Роль-Пользователь";
            public static readonly RefField<ScriptCommandGroupLink, ScriptCommandGroup> ScriptCommandGroup = new()
            {
                Name = nameof(ScriptCommandGroup),
                RefGetter = x => x.ScriptCommandGroup,
                RefIdGetter = x => x.ScriptCommandGroupId,
                RefSetter = (x, val) => x.ScriptCommandGroup = val,
                RefIdSetter = (x, val) => x.ScriptCommandGroupId = val ?? 0,
                Required = true,
                HumanName = "Группа скрипт-операций"
            };
            public static readonly RefField<ScriptCommandGroupLink, ScriptCommand> ScriptCommand = new()
            {
                Name = nameof(ScriptCommand),
                RefGetter = x => x.ScriptCommand,
                RefIdGetter = x => x.ScriptCommandId,
                RefSetter = (x, val) => x.ScriptCommand = val,
                RefIdSetter = (x, val) => x.ScriptCommandId = val ?? 0,
                Required = true,
                HumanName = "Скрипт-операция"
            };

            public static readonly List<Field<ScriptCommandGroupLink>> Fields = [ScriptCommandGroup, ScriptCommand];
        }
        #endregion
    }
}
