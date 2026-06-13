using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Frame.Tests.Shared;
using TestObject = Frame.Domain.Entities.Test.TestObject;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestAudit : IClassFixture<FrameTestFixture>
    {
        private readonly FrameTestFixture _fixture;
        public TestAudit(FrameTestFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            
            _fixture.CreateInitDatabase();
        }
        

        [Fact]
        public async Task New_Modify_Delete()
        {
            await _fixture.LoginTestuserAsync();
            
            TestObject o1 = new() { IntAttr = 1, TextAttr = "TextAttr", DateTimeAttr = new DateTime(2024, 4, 1, 10, 18, 55), DecimalAttr = new decimal(18.45) };

            AppDbContext appDbContext = _fixture.GetAppDbContext();
            appDbContext.AuditMode = true;
            // 1. Создание нового объекта
            appDbContext.TestObject.Add(o1);
            
            await appDbContext.SaveChangesAsync();

            appDbContext.AuditRecord.Count().Should().Be(1);
            AuditRecord? audit = await appDbContext.AuditRecord.FirstOrDefaultAsync();
            VerifyAuditValues(audit, o1.Id, "Added", "2024-04-01T10:18:55", "18.45", "1", "TextAttr", false);
            audit!.OldValues.Should().Be("");


            // 2. Редактирование существующего объекта // Modified
            o1.TextAttr = "Updated attr";
            o1.IntAttr = 1000;
            await appDbContext.SaveChangesAsync();
            Assert.Equal(2, appDbContext.AuditRecord.Count());
            audit = await appDbContext.AuditRecord.Where(a => a.EntityId == o1.Id && a.Action == "Modified").FirstOrDefaultAsync();
            VerifyAuditValues(audit, o1.Id, "Modified", "2024-04-01T10:18:55", "18.45", "1", "TextAttr", true);
            VerifyAuditValues(audit, o1.Id, "Modified", "2024-04-01T10:18:55", "18.45", "1000", "Updated attr", false);

            // 3. Удаление существующего объекта // Deleted
            appDbContext.Remove(o1);
            await appDbContext.SaveChangesAsync();
            Assert.Equal(3, appDbContext.AuditRecord.Count());
            audit = await appDbContext.AuditRecord.Where(a => a.EntityId == o1.Id && a.Action == "Deleted").FirstOrDefaultAsync();
            VerifyAuditValues(audit, o1.Id, "Deleted", "2024-04-01T10:18:55", "18.45", "1000", "Updated attr", true);
            appDbContext.AuditMode = false;
        }

        private static void VerifyAuditValues(AuditRecord? audit, int idVal, string strActionVal, string strDateTimeVal, string strDecimalVal, string strIntVal, string strTextVal, bool bOldValues = false)
        {
            audit.Should().NotBeNull();
            audit!.EntityId.Should().Be(idVal);
            audit.Action.Should().Be(strActionVal);
            Dictionary<string, object?>? actual;
            if (bOldValues)
            {
                actual = JsonSerializer.Deserialize<Dictionary<string, object?>>(audit.OldValues);
            }
            else
            {
                actual = JsonSerializer.Deserialize<Dictionary<string, object?>>(audit.NewValues);
            }
            actual.Should().NotBeNull();
            actual!["DateTimeAttr"]!.ToString().Should().Be(strDateTimeVal);
            actual["DecimalAttr"]!.ToString().Should().Be(strDecimalVal);
            actual["IntAttr"]!.ToString().Should().Be(strIntVal);
            actual["TextAttr"]!.ToString().Should().Be(strTextVal);
        }

    }
}
