using FluentAssertions;
using Frame.App.EntityTemplates;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Test;
using Frame.Shared;
using Microsoft.Extensions.DependencyInjection;
using TestObject = Frame.Domain.Entities.Test.TestObject;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestEntityTemplate: IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;
        public TestEntityTemplate(FrameTestFixtureExt fixture) 
        {           
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            
            if(_fixture.ServiceProvider == null) throw new FrameException("В TestFixture отсутствует ServiceProvider");
        }
        
        [Fact]
        public async Task TestSimpleSerializeAsync()
        {
            TestObject testObject = new TestObject()
            {
                Id = 84, 
                IntAttr = 5482,
                TextAttr = "hello",
                DateTimeAttr = DateTime.UtcNow
            };

            IEntityTemplateService? templateService = _fixture.ServiceProvider.GetService<IEntityTemplateService>();
            templateService.Should().NotBeNull();

            Result<string> resJson = await templateService.SerializeAsync(testObject);
            resJson.IsError.Should().BeFalse();

            TemplateSerializationContext ctx = new();
            
            Result<TestObject> resObj = await templateService.DeserializeAsync<TestObject>(resJson.Value!, ctx);
            resObj.IsError.Should().BeFalse(resObj.ErrorResult);
            TestObject? testObj = resObj.Value!;
            testObj.Should().NotBeNull();
            testObj.Id.Should().Be(0);
            testObj.TextAttr.Should().Be("hello");
            testObj.IntAttr.Should().Be(5482);
            testObj.DateTimeAttr.Should().Be(testObject.DateTimeAttr);
        }

        [Fact]
        public async Task TestComplexSerializeAsync()
        {
            IObjectStorage storage = _fixture.CreateObjectStorage();
            
            TestObject testObject = new TestObject()
            {
                IntAttr = 5482,
                TextAttr = "hello",
                DateTimeAttr = DateTime.UtcNow
            };
            TestChildObject child = new TestChildObject()
            {
                Name = "Child1",
                OwnerObject = testObject
            };
            storage.Add(testObject);
            storage.Add(child);
            Result resSave = await storage.SaveChangesAsync();
            resSave.IsError.Should().BeFalse(resSave.ErrorResult);

            IEntityTemplateService? templateService = _fixture.ServiceProvider.GetService<IEntityTemplateService>();
            templateService.Should().NotBeNull();

            Result<string> resJson = await templateService.SerializeAsync(child);
            resJson.IsError.Should().BeFalse(resJson.ErrorResult);

            // Создаем новый Storage, чтобы гарантировано читать из БД
            TemplateSerializationContext ctx = new()
            {
                ObjectStorage = _fixture.CreateObjectStorage()
            };
            
            Result<TestChildObject> resObj = await templateService.DeserializeAsync<TestChildObject>(resJson.Value!, ctx);
            resObj.IsError.Should().BeFalse(resObj.ErrorResult);
            
            TestChildObject? restoredChild = resObj.Value!;
            restoredChild.Should().NotBeNull();
            restoredChild.Id.Should().Be(0);
            restoredChild.OwnerObjectId.Should().Be(testObject.Id);
            restoredChild.OwnerObject.Should().NotBeNull("Похоже объект не считался из БД");
            restoredChild.OwnerObject.TextAttr.Should().Be("hello");
            restoredChild.OwnerObject.IntAttr.Should().Be(5482);
            restoredChild.OwnerObject.DateTimeAttr.Should().Be(testObject.DateTimeAttr);
        }
    }
}
