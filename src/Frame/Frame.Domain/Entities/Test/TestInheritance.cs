using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Test
{
    public class TestBaseClass : BaseEntity, IBaseGenericEntity<TestBaseClass>
    {
        public List<Field<TestBaseClass>> GetFields()
        {
            return [];
        }

        public static class Meta
        {
            public static readonly string HumanName = "TestBaseClass";
        }

    }

    public class TestDerivedClass : TestBaseClass
    {
        public string Name { get; set; } = "";
        public DateTime DateAttr { get; set; } = DateTime.UtcNow;
        
        public static class Meta
        {
            public static readonly string HumanName = "TestDerivedClass";
        }
    }
}
