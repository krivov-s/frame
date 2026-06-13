using System.Reflection;

namespace Frame.Domain.Entities.Core.Reports
{
    public class FrameReportGroup : BaseEntity, IBaseGenericEntity<FrameReportGroup>
    {
        public override string Description => Name; 

        public string Name { get; set; } = string.Empty;
        public string Descr { get; set; } = "";
        public List<FrameReportGroupLink> FrameReports { get; set; } = [];

        List<Field<FrameReportGroup>> IBaseGenericEntity<FrameReportGroup>.GetFields() { return Meta.Fields; }

        public static class Meta
        {
            public static readonly string HumanName = "Группа сохранённых отчётов";
            public static readonly Field<FrameReportGroup> Name = new()
            {
                Name = nameof(Name),
                StringGet = x => x.Name,
                StringSet = (x, val) => x.Name = val ?? "",
                Required = true,
                MaxLength = 80,
                HumanName = "Название группы сохранённых отчётов"
            };
            public static readonly Field<FrameReportGroup> Descr = new()
            {
                Name = nameof(Descr),
                StringGet = x => x.Descr,
                StringSet = (x, val) => x.Descr = val ?? "",
                Required = false,
                MaxLength = 2048,
                HumanName = "Описание"
            };

            public static readonly List<Field<FrameReportGroup>> Fields = [Name, Descr];
        }
    }
}
