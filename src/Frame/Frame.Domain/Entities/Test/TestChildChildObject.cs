using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Test
{
    public class TestChildChildObject : BaseEntity, IBaseGenericEntity<TestChildChildObject>
    {
        public override string Description => Name;

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        virtual public TestChildObject? OwnerChild { get; set; }
        public int OwnerChildId { get; set; }

        //public virtual List<TestChildObject>? Leaves { get; set; } = new();

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Подчиненный подчиненному";
            public static readonly Field<TestChildChildObject> Name = new() { Name = nameof(Name), StringGet = f => f.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 80, HumanName = "Наименование" };
            public static readonly Field<TestChildChildObject> Comment = new() { Name = nameof(Comment), StringGet = f => f.Comment, StringSet = (x, val) => x.Comment = val ?? "", Required = true, MaxLength = 255, HumanName = "Примечание" };
            public static readonly RefField<TestChildChildObject, TestChildObject> OwnerChild = new() { 
                Name = nameof(OwnerChild), 
                RefGetter = f => f.OwnerChild, 
                RefSetter = (x, val) => x.OwnerChild = val, 
                Required = true, 
                HumanName = "Объект-владелец (child)" };

            public static readonly List<Field<TestChildChildObject>> Fields = [Name, Comment, OwnerChild];
        }
        public List<Field<TestChildChildObject>> GetFields() { return Meta.Fields; }
        #endregion
    }
}
