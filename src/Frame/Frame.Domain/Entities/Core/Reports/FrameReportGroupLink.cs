namespace Frame.Domain.Entities.Core.Reports
{
    public class FrameReportGroupLink : BaseEntity, IBaseGenericEntity<FrameReportGroupLink>
    {
        public override string Description => "Связка Группа отчётов - Отчёт";

        virtual public FrameReportGroup? FrameReportGroup { get; set; }
        public int FrameReportGroupId { get; set; }
        virtual public FrameReport? FrameReport { get; set; }
        public int FrameReportId { get; set; }

        public List<Field<FrameReportGroupLink>> GetFields() { return Meta.Fields; }

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Связка Роль-Пользователь";
            public static readonly RefField<FrameReportGroupLink, FrameReportGroup> FrameReportGroup = new()
            {
                Name = nameof(FrameReportGroup),
                RefGetter = x => x.FrameReportGroup,
                RefIdGetter = x => x.FrameReportGroupId,
                RefSetter = (x, val) => x.FrameReportGroup = val,
                RefIdSetter = (x, val) => x.FrameReportGroupId = val ?? 0,
                Required = true,
                HumanName = "Группа отчётов"
            };
            public static readonly RefField<FrameReportGroupLink, FrameReport> FrameReport = new()
            {
                Name = nameof(FrameReport),
                RefGetter = x => x.FrameReport,
                RefIdGetter = x => x.FrameReportId,
                RefSetter = (x, val) => x.FrameReport = val,
                RefIdSetter = (x, val) => x.FrameReportId = val ?? 0,
                Required = true,
                HumanName = "Отчёт"
            };

            public static readonly List<Field<FrameReportGroupLink>> Fields = [FrameReportGroup, FrameReport];
        }
        #endregion
    }
}