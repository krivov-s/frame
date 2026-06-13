using FluentAssertions;
using TestObject = Frame.Domain.Entities.Test.TestObject;
using Frame.App.IEntityRepositories;
using Frame.Shared;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestTransaction : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;
        public TestTransaction(FrameTestFixtureExt fixture) 
        {           
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        [Fact]
        public async Task Test_RollBack()
        {
            Result res;

            TestObject testObject = new TestObject()
            {
                IntAttr = 5482,
                TextAttr = "hello",
                DateTimeAttr = DateTime.UtcNow
            };
            IObjectStorage storage = _fixture.CreateObjectStorage();
            storage.Add(testObject);

            await storage.BeginTransactionAsync();

            res = await storage.SaveChangesAsync();

            res.IsError.Should().BeFalse(res.ErrorResult);
            testObject.Id.Should().BePositive();

            IQueryable<TestObject> query = storage.GetQuery<TestObject>().Value!;
            TestObject? testObjectT = query.Where(x => x.Id == testObject.Id).FirstOrDefault();
            testObjectT.Should().NotBeNull();

            res = await storage.RollbackTransactionAsync();
            res.IsError.Should().BeFalse();

            testObjectT = (await storage.GetObjAsync<TestObject>(testObject.Id)).Value!;
            testObjectT.Should().BeNull();
        }
    }
}
