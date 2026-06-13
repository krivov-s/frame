using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Frame.Domain.Entities.Test;
using Frame.Infrastructure.DBContext;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestSimpleEntities : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;

        private readonly AppDbContext _appDbContext;
        
        public TestSimpleEntities(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _appDbContext = _fixture.GetAppDbContext() ??
                            throw new Exception("Ошибка получения AppDbContext от FrameTestFixtureExt");
        }

        [Fact]
        public async Task TestAssocDefaultCreation()
        {
            TestChildObject child = new();
            child.Name = "test";
            _appDbContext.Add(child);
            // Должна быть ошибка сохранения
            await Assert.ThrowsAsync<DbUpdateException>(() => _appDbContext.SaveChangesAsync());
            child.Id.Should().Be(0);
            child.OwnerObjectId.Should().Be(0);
            child.OwnerObject.Should().BeNull();
        }

    }
}
