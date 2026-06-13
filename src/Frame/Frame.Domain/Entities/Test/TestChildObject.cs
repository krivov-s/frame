
using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Test
{
    public class TestChildObject : BaseEntity, IBaseGenericEntity<TestChildObject>
    {
        public override string Description => Name;

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public virtual TestObject? OwnerObject { get; set; }
        public int OwnerObjectId { get; set; }
        public virtual TestChildObject? TreeParent { get; set; }
        public int? TreeParentId { get; set; }

        public virtual List<TestChildChildObject> ChildChildObjects { get; set; } = [];

        //public virtual List<TestChildObject>? Leaves { get; set; } = new();

        #region Metadata
        public static class Meta
        {
            public static readonly string HumanName = "Подчиненный объект";
            public static readonly Field<TestChildObject> Name = new() { Name = nameof(Name), StringGet = f => f.Name, StringSet = (x, val) => x.Name = val ?? "", Required = true, MaxLength = 80, HumanName = "Наименование" };
            public static readonly Field<TestChildObject> Comment = new() { Name = nameof(Comment), StringGet = f => f.Comment, StringSet = (x, val) => x.Comment = val ?? "", Required = false, MaxLength = 255, HumanName = "Примечание" };
            public static readonly RefField<TestChildObject, TestObject> OwnerObject = new() { 
                Name = nameof(OwnerObject), 
                RefGetter = f => f.OwnerObject, 
                RefIdGetter = f => f.OwnerObjectId, 
                RefSetter = (x, val) => x.OwnerObject = val, 
                RefIdSetter = (x, val) => x.OwnerObjectId = val ?? 0, 
                Required = true, 
                HumanName = "Объект-владелец" };
            public static readonly RefField<TestChildObject, TestChildObject> TreeParent = new()
            {
                Name = nameof(TreeParent),
                RefGetter = f => f.TreeParent,
                RefIdGetter = f => f.TreeParentId,
                RefSetter = (x, val) => x.TreeParent = val,
                RefIdSetter = (x, val) => x.TreeParentId = val,
                Required = false,
                HumanName = "Узел-владелец"
            };

            public static readonly List<Field<TestChildObject>> Fields = [Name, Comment, OwnerObject, TreeParent];
        }
        public List<Field<TestChildObject>> GetFields() { return Meta.Fields; }
        #endregion
    }
}
