using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Frame.Domain.Entities.Test;
using Frame.Domain.Entities.Core.Security;
using Frame.Infrastructure.DBContext;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestSet : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;

        private readonly AppDbContext _appDbContext;
        private readonly NoSecurityDbContext _noSecurityDbContext;
        
        public TestSet(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _appDbContext = _fixture.GetAppDbContext() ??
                            throw new Exception("Ошибка получения AppDbContext от FrameTestFixtureExt");
            _noSecurityDbContext = _fixture.GetNoSecurityDbContext() ??
                                   throw new Exception("Ошибка получения NoSecurityDbContext от FrameTestFixtureExt");
        }
        
        
        [Fact]
        public async Task TestContextTwoSets()
        {
            List<User> users = await _appDbContext.GetSet<User>().ToListAsync();

            // Добавить нового 
            User user = new() { Login = "xxx", NormalizedLogin = "XXX", FIO = "X.X.X" };
            _appDbContext.Add(user);
            int iChanges = await _appDbContext.SaveChangesAsync();
            iChanges.Should().Be(1);

            // Проверка в том же контексте
            User? user_app_ctx = await _appDbContext.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync();    //.AsNoTracking()
            user_app_ctx.Should().NotBeNull();

            // Проверка в другом уже существующем контексте
            User? user_sec_ctx = await _noSecurityDbContext.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync(); //.AsNoTracking()
            user_sec_ctx.Should().NotBeNull();

            // Редактировать в первоначальном контексте
            user_app_ctx!.FIO = "XXXXX";
            iChanges = await _appDbContext.SaveChangesAsync();
            iChanges.Should().Be(2);  // Основной объект + Аудит

            // Второй контекст изменений не видит
            user_sec_ctx = await _noSecurityDbContext.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync();    //.AsNoTracking()
            user_sec_ctx.Should().NotBeNull();
            user_sec_ctx!.FIO.Should().Be("X.X.X");

            // Новый контекст изменения *видит*
            NoSecurityDbContext freshCtx0 = _fixture.GetNoSecurityDbContext();
            freshCtx0.Should().NotBeNull();
            User? user_ctx6 = await freshCtx0.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync();
            user_ctx6.Should().NotBeNull();
            user_ctx6!.FIO.Should().Be("XXXXX");

            // Второй контекст с дополнительным AsNoTracking() изменения *видит*
            User? user_ctx3 = await _noSecurityDbContext.GetSet<User>().Where(u => u.Login == "xxx").AsNoTracking().FirstOrDefaultAsync();
            user_ctx3.Should().NotBeNull();
            user_ctx3!.FIO.Should().Be("XXXXX");

            NoSecurityDbContext freshCtx1 = _fixture.GetNoSecurityDbContext();
            freshCtx1.Should().NotBeNull();
            User? user_ctx4 = await freshCtx1.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync();
            user_ctx4.Should().NotBeNull();
            user_ctx4!.FIO.Should().Be("XXXXX");

            _noSecurityDbContext.Entry(user_sec_ctx).Reload();
            user_sec_ctx.FIO.Should().Be("XXXXX");

            NoSecurityDbContext freshCtx2 = _fixture.GetNoSecurityDbContext();
            freshCtx2.Should().NotBeNull();
            User? user_ctx5 = await freshCtx2.GetSet<User>().Where(u => u.Login == "xxx").FirstOrDefaultAsync();
            user_ctx5.Should().NotBeNull();
            user_ctx5!.FIO.Should().Be("XXXXX");
            //_appDbContext.Ch
        }

        [Fact]
        public async Task TestLinqDynamic()
        {
            List<User> usersAll = await _noSecurityDbContext.User.ToListAsync();
            
            List<User> users = await _noSecurityDbContext.User.Where("Login = \"testuser\"").ToListAsync();
            users.Should().NotBeEmpty();
            users.Count.Should().Be(1);
            users = await _noSecurityDbContext.User.Where(@" FIO="""" ").ToListAsync();
            users.Should().NotBeEmpty();
            users.Count.Should().Be(3); //2 обычных пользователя и system
            users = await _noSecurityDbContext.User.Where("fio == null").ToListAsync();
            users.Should().BeEmpty();
            string condition = "Login = \"testuser\" and FIO != \"\"";
            users = await _noSecurityDbContext.User.Where(condition).ToListAsync();
            users.Should().BeEmpty();
            users.Count.Should().Be(0);
            condition = "Login = \"testuser\" and FIO == \"\"";
            users = await _noSecurityDbContext.User.Where(condition).ToListAsync();
            users.Should().NotBeEmpty();
            users.Count.Should().Be(1);
        }

        [Fact]
        public async Task TestInheritance()
        {
            TestDerivedClass d1 = new() { Id = 1, Name = "aaa", DateAttr = new DateTime(2024, 1, 1, 18, 0, 0).ToUniversalTime() };
            TestDerivedClass d2 = new() { Id = 2, Name = "bbb" };
            _appDbContext.Add(d1);
            _appDbContext.Add(d2);
            await _appDbContext.SaveChangesAsync();

            TestDerivedClass rd1 = await _appDbContext.GetSet<TestDerivedClass>().FirstAsync();
            rd1.Id.Should().Be(d1.Id);
            rd1.Name.Should().Be(d1.Name);
        }

        [Fact]
        public async Task TestDateTimeUtc()
        {
            TestDerivedClass d1 = new() { Name = "xxx", DateAttr = new DateTime(2024, 12, 8, 12, 25, 0) };
            _appDbContext.Add(d1);
            await Assert.ThrowsAsync<DbUpdateException>(() => _appDbContext.SaveChangesAsync());

            d1.DateAttr = new DateTime(2024, 12, 8, 12, 25, 0, DateTimeKind.Utc);
            await _appDbContext.SaveChangesAsync();

            TestDerivedClass rd1 = await _appDbContext.GetSet<TestDerivedClass>().Where("Name == \"xxx\"").FirstAsync();
            rd1.Id.Should().Be(d1.Id);
            rd1.Name.Should().Be(d1.Name);
            rd1.DateAttr.Should().Be(d1.DateAttr);
        }

        [Fact]
        public void Set_Should_Be_Blocked()
        {
            try
            {
                DbSet<User> setu = _appDbContext.Set<User>();
                Assert.Fail("Не сработал Exception при вызове Set<User>()");
            }
            catch (Exception)
            {
            }
        }


        [Fact]
        public async Task TestGenericGetSet()
        {
            IQueryable<User> userIQuer = _appDbContext.GetSet<User>();
            userIQuer = userIQuer.Where(p => p.Id < 10);
            userIQuer = userIQuer.Where(p => p.Login == "Tom");
            userIQuer = userIQuer.Where("UserClaims.Any(ClaimValue = \"user@rshb.ru\")");
            List<User> users = await userIQuer.ToListAsync();
            users.Count.Should().Be(0);

            IQueryable<User> userIQuer2 = _appDbContext.GetSet<User>();
            userIQuer2 = userIQuer2.Where(p => p.Id < 10);
            userIQuer2 = userIQuer2.Where(p => p.Login == "Tom");
            userIQuer2 = userIQuer2.Where("UsersRoles.Any(Role.Name = \"admin\")");
            List<User> users2 = await userIQuer2.ToListAsync();
            users2.Count.Should().Be(0);

            IQueryable<TEntityRights> er = _appDbContext.GetSet<TEntityRights>();
            await er.ToListAsync();
        }

        [Fact]
        public void TestChildObjectInContext()
        {
            TestObject testObject = new TestObject();
            _appDbContext.Add(testObject);
            testObject.ChildObjects.Add(new TestChildObject() { OwnerObject = testObject, Name = "1st child" });

            _appDbContext.ChangeTracker.Entries().Count().Should().Be(2);

            TestObject testObject2 = new TestObject();
            _appDbContext.ChangeTracker.Entries().Count().Should().Be(2);

            _appDbContext.Add(testObject2);
            _appDbContext.ChangeTracker.Entries().Count().Should().Be(3);

            testObject2.ChildObjects.Add(new TestChildObject() { OwnerObject = testObject2, Name = "2nd child" });
            _appDbContext.ChangeTracker.Entries().Count().Should().Be(4);

            TestObject testObject3 = new TestObject();
            testObject3.ChildObjects.Add(new TestChildObject() { OwnerObject = testObject3, Name = "3nd child" });
            _appDbContext.ChangeTracker.Entries().Count().Should().Be(4);

            _appDbContext.Add(testObject3);
            _appDbContext.ChangeTracker.Entries().Count().Should().Be(6);
        }

        //[Fact]
        //public async Task TestEagerLazyLoading()
        //{
        //    EFBaseRepository<TestObject> repo = CreateBaseRepository<TestObject>();

        //    TestObject root = new() { IntAttr = 1, TextAttr = "Root object" };

        //    // Уровень вложенности 1
        //    TestChildObject child1 = new() { Name = "Child 1", OwnerObject = root } ;
        //    TestChildObject child2 = new() { Name = "Child 2", OwnerObject = root };
        //    TestChildObject child3 = new() { Name = "Child 3", OwnerObject = root };

        //    root.ChildObjects.Add(child1);
        //    root.ChildObjects.Add(child2); 
        //    root.ChildObjects.Add(child3);

        //    // Уровень вложенности 2
        //    TestChildChildObject child11 = new() { Name = "Child 11", OwnerChild = child1 };
        //    child1.ChildChildObjects.Add(child11);

        //    TestChildChildObject child21 = new() { Name = "Child 21", OwnerChild = child2 };
        //    child2.ChildChildObjects.Add(child21);

        //    // Дерево в рамках TestChildObject на уровне вложенности 1
        //    TestChildObject child4 = new() { Name = "Child 4", OwnerObject = root, TreeParent = child2 };
        //    TestChildObject child5 = new() { Name = "Child 5", OwnerObject = root, TreeParent = child4 };

        //    root.ChildObjects.Add(child4);
        //    root.ChildObjects.Add(child5);  

        //    // Сохраняем все
        //    Result res = await repo.AddAsync(root);
        //    res.IsError.Should().BeFalse();

        //    IQueryable<TestChildChildObject> qryChildChild = _appDbContext.GetSet<TestChildChildObject>();
        //    List<TestChildChildObject> listChildChild = await qryChildChild.ToListAsync();
        //    listChildChild.Should().HaveCount(2);

        //    TestChildObject ownerOnLevel1 = listChildChild[0].OwnerChild;
        //    ownerOnLevel1.Should().NotBeNull();

        //    TestObject ownerRoot = ownerOnLevel1.OwnerObject;
        //    ownerRoot.Should().NotBeNull();

        //    //IQueryable<TestChildObject> qryChild = _appDbContext.GetSet<TestChildObject>();
        //    //qryChild = qryChild.Where(x => x.DocumentName == "Child 5");    // Include(qry => qry.OwnerObject).
        //    //List<TestChildObject> list = await qryChild.ToListAsync();
        //    //list.Should().HaveCount(1);
        //    //TestChildObject _child5 = list[0];
        //    //TestChildObject _child4 = _child5.TreeParent!;
        //    //_child4.Should().NotBeNull();
        //    //TestChildObject _child2 = _child4.TreeParent!;
        //    //_child2.Should().NotBeNull();
        //}
    }
    }
