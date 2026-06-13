using FluentAssertions;
using Frame.Domain.Entities.Metadata;
using TestObject = Frame.Domain.Entities.Test.TestObject;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestBaseEntity
    {
        [Fact]
        public void Compare_Null_Ret_False()
        {
            TestObject o1 = new() { Id = 0, IntAttr = 1, TextAttr = "Object 1" };
            bool result = o1.Equals(null);
            result.Should().BeFalse();
            result = o1 == null;
            result.Should().BeFalse();
        }

        [Fact]
        public void Compare_Id_0_Ret_False()
        {
            TestObject o1 = new() { Id = 0, IntAttr = 1, TextAttr = "Object 1" };
            TestObject o2 = new() { Id = 0, IntAttr = 2, TextAttr = "Object 2" };
            bool result = o1.Equals(o2);
            result.Should().BeFalse();
            result = o1 == o2;
            result.Should().BeFalse();
        }

        [Fact]
        public void Compare_Diff_Id_Ret_False()
        {
            TestObject o1 = new() { Id = 0, IntAttr = 1, TextAttr = "Object 1" };
            TestObject o2 = new() { Id = 1, IntAttr = 2, TextAttr = "Object 2" };
            bool result = o1.Equals(o2);
            result.Should().BeFalse();
            result = o1 == o2;
            result.Should().BeFalse();
        }

        [Fact]
        public void Compare_Same_Id_Ret_True()
        {
            TestObject o1 = new() { Id = 1, IntAttr = 1, TextAttr = "Object 1" };
            TestObject o2 = new() { Id = 1, IntAttr = 2, TextAttr = "Object 2" };
            bool result = o1.Equals(o2);
            result.Should().BeTrue();
            result = o1 == o2;
            result.Should().BeTrue();
        }

        [Fact]
        public void Get_Desc_Classnames_Ret_True()
        {
            List<string> result = EntityMetadata.GetAllEntitiesClassNames();
            result.Count.Should().BePositive();
            result = EntityMetadata.GetAllEntitiesClassNames();
            result.Count.Should().BePositive();
        }

        [Fact]
        public void Get_Key()
        {
            TestObject o1 = new() { Id = 1, IntAttr = 1, TextAttr = "Object 1" };
            o1.Key.Should().Be("testobject_1");
        }
    }
}
