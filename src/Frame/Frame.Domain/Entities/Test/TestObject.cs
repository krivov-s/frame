using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Test
{
    public class TestObject : BaseEntity, IBaseGenericEntity<TestObject>
    {
        public override string Description => TextAttr;

        public string TextAttr { get; set; } = string.Empty;
        public DateTime? DateTimeAttr { get; set; } = default;
        public int IntAttr { get; set; } = default;
        public decimal DecimalAttr { get; set; } = default;
        virtual public List<TestChildObject> ChildObjects { get; set; } = [];

        public int CalcAttrInt => IntAttr;
        public string CalcAttrText => TextAttr;
        
        
        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Тестовый объект";
            public static readonly Field<TestObject> TextAttr = new()
            {
                Name = nameof(TextAttr), 
                StringGet = f => f.TextAttr, 
                StringSet = (x, val) => x.TextAttr = val ?? "", 
                Required = false, 
                MaxLength = 10, 
                HumanName = "Текст"
            };
            public static readonly Field<TestObject> DateTimeAttr = new() { Name = nameof(DateTimeAttr), DateTimeGet = f => f.DateTimeAttr, Required = true, HumanName = "Дата-время" };
            public static readonly Field<TestObject> IntAttr = new() { Name = nameof(IntAttr), IntGet = f => f.IntAttr, Required = false, HumanName = "Целое" };
            public static readonly Field<TestObject> DecimalAttr = new() { Name = nameof(DecimalAttr), DecimalGet = f => f.DecimalAttr, Required = false, HumanName = "Целое" };

            public static readonly Field<TestObject> CalcAttrInt = new() { Name = nameof(CalcAttrInt), IntGet = f => f.CalcAttrInt, Required = false, HumanName = "Целое вычислимое", IsPersistent = false};
            public static readonly Field<TestObject> CalcAttrText = new() { Name = nameof(CalcAttrText), StringGet = f => f.CalcAttrText, Required = false, HumanName = "Текстовое вычислимое", IsPersistent = false};
            
            public static readonly List<Field<TestObject>> Fields = [TextAttr,DateTimeAttr, IntAttr, DecimalAttr, CalcAttrInt, CalcAttrText];
        }
        public List<Field<TestObject>> GetFields() { return Meta.Fields; }
        #endregion
    }
}
