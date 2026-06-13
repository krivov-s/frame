using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Frame.App.Cores;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Test;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using System.Linq.Expressions;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestSecurityQueries : IClassFixture<FrameTestFixtureExt>, IAsyncLifetime
    {
        private readonly FrameTestFixtureExt _fixture;

        private AppDbContext _appDbContext;
        private NoSecurityDbContext _noSecurityDbContext;

        public TestSecurityQueries(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _fixture.OnOneTimeSetUpAsync = CreateTestDataAsync;
            _appDbContext = _fixture.GetAppDbContext() ??
                            throw new Exception("Ошибка получения AppDbContext от FrameTestFixture");
            _noSecurityDbContext = _fixture.GetNoSecurityDbContext() ??
                                   throw new Exception("Ошибка получения NoSecurityDbContext от FrameTestFixture");
        }
        
        [Fact]
        public async Task Read_TestObject_Testuser_ret_1()
        {
            // Всего в БД 3 объекта типа TestObject
            // testuser: User
            await _fixture.RecreateDatabaseAsync();
            List<TestObject> listTest = await _fixture.GetAppDbContext().GetSet<TestObject>().ToListAsync();
            listTest.Count.Should().Be(1);
        }


        [Fact]
        public async Task Read_TestChildObject_Testuser_ret_2()
        {
            // testuser: User
            await _fixture.RecreateDatabaseAsync();
            List<TestChildObject> listTest = await _appDbContext.GetSet<TestChildObject>().ToListAsync();
            listTest.Count.Should().Be(2);
        }


        [Fact]
        public async Task Set_Simple_Filter()
        {
            try
            {
                await _fixture.RecreateDatabaseAsync();

                // Простые фильтры работают нормально (оптимально). Лишние Where в SQL не генерят. Можно активно использовать!
                DbSet<User> set1 = _noSecurityDbContext.Set<User>();
                List<User> users = [.. set1.Where("True")];

                DbSet<User> set2 = _noSecurityDbContext.Set<User>();
                users = [.. set2.Where("False")];

                DbSet<User> set3 = _noSecurityDbContext.Set<User>();
                users = [.. set3.Where("(True) OR (Login = \"testuser\")")];

                DbSet<User> set4 = _noSecurityDbContext.Set<User>();
                users = [.. set4.Where("(False) OR (Login = \"testuser\")")];
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [Fact]
        public async Task Check_Object_Condition()
        {
            await _fixture.RecreateDatabaseAsync();
            
            IUserCore userCore = _fixture.GetUserCore();

            TestObject o_Test = new() { IntAttr = 1, TextAttr = "This is Test" };
            TestObject o_no_Test = new() { IntAttr = 1, TextAttr = "Здесь нет нужного слова" };

            // ==================================================================
            // Проверка на уровне объектов
            // ==================================================================
            bool canAdd_obj_user_true = userCore.CanAdd(o_Test);
            canAdd_obj_user_true.Should().BeFalse("OLS позволяет, но TLS запрещает - поэтому должно быть false");

            bool canModify_obj_user_false = userCore.CanModify(o_Test);
            canModify_obj_user_false.Should().BeFalse("TLS позволяет, но OLS запрещает - поэтому false");
        }

        [Fact]
        public async Task Check_EntityType_Condition()
        {
            await _fixture.RecreateDatabaseAsync();
            // ==================================================================
            // Проверка на уровне типов
            // ==================================================================
            IUserCore userCore = _fixture.GetUserCore();

            bool canAdd_user_false = userCore.CanAdd<TestObject>();
            canAdd_user_false.Should().BeFalse(@"CreateTLS = ""appCore.CurrentUser.Login == testuser2"" ");

            bool canModify_user_true = userCore.CanModify<TestObject>();
            canModify_user_true.Should().BeTrue(@"ModifyTLS = ""appCore.CurrentUser.Login == ""testuser"" ");

            bool can_delete_obj_user_true = userCore.CanDelete<TestObject>();
            can_delete_obj_user_true.Should().BeTrue(@"DeleteTLS = ""appCore.CurrentUser.Login == testuser"" ");
        }

        [Fact]
        public async Task User_Cant_Create_async()
        {
            await _fixture.RecreateDatabaseAsync();
            TestObject o = new() { TextAttr = "Хрен тебе а не создание объекта" };
            _appDbContext.Add(o);
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task User_Can_Create_async()
        {
            await _fixture.RecreateDatabaseAsync();
            TestObject o = new() { TextAttr = "Нельзя, хотя есть Test, потому что TLS - только testuser2" };
            _appDbContext.Add(o);
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task User_Cant_Modify_async()
        {
            await _fixture.RecreateDatabaseAsync();
            TestObject? o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            o!.TextAttr = "TLS разрешит, поскольку нужный пользователь, OLS - запрет, т.к. нет слова В_а_с_я";
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task User_Can_Modify_async()
        {
            await _fixture.RecreateDatabaseAsync();
            TestObject? o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            o!.TextAttr = "TLS разрешит, поскольку testuser, OLS разрешит поскольку есть Вася";
            int iSavedCount = await _appDbContext.SaveChangesAsync();
            iSavedCount.Should().Be(2); // основной объект + аудит
            o.TextAttr = "TLS разрешит, а OLS запретит";
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task User_Cant_Delete_async()
        {
            await _fixture.RecreateDatabaseAsync();
            // Удаление не пройдет из-за OLS (только testuser2)
            TestObject? o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            _appDbContext.Remove(o!);
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());

            // Попытка обхитрить OLS путем изменения атрибутов объекта "на лету" также не прокатит:
            // на это стоит специальная защита на уровне AppDbContext
            o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            o!.TextAttr = "testuser2 CanDelete";
            _appDbContext.Remove(o);
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task User_Can_Delete_async()
        {
            await _fixture.RecreateDatabaseAsync();
            TestObject? o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            o!.TextAttr = "В описании есть Вася";
            int iSavedCoount = await _appDbContext.SaveChangesAsync();
            iSavedCoount.Should().Be(2); // Основной объект + Аудит

            o = await _appDbContext.GetSet<TestObject>().FirstOrDefaultAsync();
            o.Should().NotBeNull();
            _appDbContext.Remove(o!);
            // Удаление основного объекта проходит, а лажает он на дочернем, на которой прав на удаление нет!!!
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task Test_Typed_Expressions()
        {
            await _fixture.RecreateDatabaseAsync();
            Type tTestObject = typeof(TestObject);
            ParameterExpression entityParam = Expression.Parameter(tTestObject, "obj");
            ParameterExpression appCoreParam = Expression.Parameter(typeof(AppCore), "appCore");

            LambdaExpression expression = DynamicExpressionParser.ParseLambda(
                [entityParam, appCoreParam],
                typeof(bool),
                """obj.TextAttr == "Значение 1" """);

            var compiledExpression = expression.Compile();
            compiledExpression.Should().NotBeNull();

            // Func<TestObject, bool> func = (Func<TestObject, bool>)expression.Compile();
            // Unable to cast object of type 'System.Func`2[Frame.Domain.Entities.Test.TestObject,System.Boolean]' to type 'System.Func`2[System.Object,System.Boolean]'.
            TestObject o1 = new() { TextAttr = "Значение 1" };
            TestObject o2 = new() { TextAttr = "Значение 2" };

            bool res1 = (bool)compiledExpression!.DynamicInvoke(o1, null)!;
            res1.Should().BeTrue();

            Func<TestObject, AppCore?, bool> func = (Func<TestObject, AppCore?, bool>)compiledExpression;
            func.Should().NotBeNull();
            res1 = func.Invoke(o1, null);
            res1.Should().BeTrue();

            bool res2 = (bool)compiledExpression.DynamicInvoke(o2, null)!;
            res2.Should().BeFalse();

            User u = new();
            Assert.ThrowsAny<ArgumentException>(() => compiledExpression.DynamicInvoke(u, null));
        }

        [Fact]
        public async Task User_Cant_Change_Attrs_Async()
        {
            TestChildObject? tc = await _appDbContext.GetSet<TestChildObject>().FirstOrDefaultAsync();
            tc.Should().NotBeNull();
            
            TestChildChildObject tcc = new TestChildChildObject()
            {
                Name = "Name1",
                Comment = "Comment1",
                OwnerChild = tc
            };

            // Добавление должно пройти нормально
            _appDbContext.Add(tcc);
            await _appDbContext.SaveChangesAsync();
            
            // Изменение должно вывалиться с ошибкой
            tcc.Name = "Name2";
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
            
            // Без изменения - норм
            tcc.Name = "Name1";
            await _appDbContext.SaveChangesAsync();

            // Изменение второго поля тоже должно вывалиться с ошибкой
            tcc.Comment = "New comment";
            await Assert.ThrowsAnyAsync<FrameSecurityException>(() => _appDbContext.SaveChangesAsync());
        }

        private async Task CreateTestDataAsync()
        {
            // Заново получаем контексты для новой БД
            _appDbContext = _fixture.GetAppDbContext() ??
                            throw new Exception("Ошибка получения AppDbContext от FrameTestFixture");
            _noSecurityDbContext = _fixture.GetNoSecurityDbContext() ??
                                   throw new Exception("Ошибка получения NoSecurityDbContext от FrameTestFixture");
            
            // Пользователю testuser добавляем роль User

            User user = _fixture.GetTestuser();
            user.Should().NotBeNull();
            
            Role roleUser = new() { Name = "User" };
            _appDbContext.Add(roleUser);
            await _appDbContext.SaveChangesAsync();

            UsersRoles userRoleRel = new() { RoleId = roleUser.Id, UserId = user.Id };
            _appDbContext.Add(userRoleRel);
            await _appDbContext.SaveChangesAsync();
            
            TestObject baseObject1 = new() { TextAttr = "Test object 1", DecimalAttr = 10 };
            TestObject baseObject2 = new() { TextAttr = "Test object 2", DecimalAttr = 20 };
            TestObject baseObject3 = new() { TextAttr = "Real object", DecimalAttr = 99 };
            TestChildObject to11 = new() { Name = "Дочка 1.1", OwnerObject = baseObject1 };
            TestChildObject to12 = new() { Name = "Дочка 1.2", OwnerObject = baseObject1 };
            TestChildObject to21 = new() { Name = "Дочка 2.1", OwnerObject = baseObject2 };
            TestChildObject to22 = new() { Name = "Дочка 2.2", OwnerObject = baseObject2 };
            TestChildObject to31 = new() { Name = "Дочка 3.1", OwnerObject = baseObject3 };
            TestChildObject to32 = new() { Name = "Дочка 3.2", OwnerObject = baseObject3 };
            
            _appDbContext.Add(baseObject1);
            _appDbContext.Add(baseObject2);
            _appDbContext.Add(baseObject3);
            _appDbContext.Add(to11);
            _appDbContext.Add(to12);
            _appDbContext.Add(to21);
            _appDbContext.Add(to22);
            _appDbContext.Add(to31);
            _appDbContext.Add(to32);

            // Сохранение тестовых объектов
            await _appDbContext.SaveChangesAsync();

            // Role? roleAdmin = await _appDbContext.GetSet<Role>().Where(r => r.Name == "Admin").FirstOrDefaultAsync();
            // roleAdmin.Should().NotBeNull();
            // Role? roleUser = await _appDbContext.GetSet<Role>().Where(r => r.Name == "User").FirstOrDefaultAsync();
            // roleUser.Should().NotBeNull();

            // -----------------------------------------------------------------
            // Роли User добавляем ограничения:
            // -----------------------------------------------------------------
            // Роль User для типа TestObject:
            // -----------------------------------------------------------------
            // - можно читать только объекты, в названии которых есть "Real"
            // - можно изменить только объекты, в названии которых есть "Test"
            TEntityRights er1 = new()
            {
                EntityTypeName = nameof(TestObject),
                ReadQueryFilter = "TextAttr.Contains(\"Real\") " +
                                  "or TextAttr.Contains(\"test\") " +
                                  "or TextAttr.Contains(\"Вася\")",
                CreateTLS = """appCore.CurrentUser.Login == "testuser2" """,
                CreateOLS = """obj.TextAttr.Contains("Test")""",
                ModifyTLS = """appCore.CurrentUser.Login == "testuser" """,
                ModifyOLS = """obj.TextAttr.Contains("Вася")""",
                DeleteTLS = """appCore.CurrentUser.Login == "testuser" """,
                DeleteOLS = """obj.TextAttr.Contains("Вася")""",
                ReadAttrs = "DateTimeAttr",
                CanReadAttrs = false,
                ModifyAttrs = "DateTimeAttr",
                CanModifyAttrs = false,
                RoleId = roleUser!.Id
            };

            _appDbContext.Add(er1);
            await _appDbContext.SaveChangesAsync();
            //Result<TEntityRights> r1 = await _entityRightsRepository.AddAsync(er1);
            //r1.IsError.Should().BeFalse();

            // -----------------------------------------------------------------
            // Роль User для типа TestChildObject:
            // -----------------------------------------------------------------
            // - можно читать только объекты, в названии родительского объекта у которых есть "Real"
            // - можно изменить только объекты, у которых у родительского объекта DecimalAttr = 10
            // - можно удалять только объекты, у которых у родительского объекта DecimalAttr = 20
            TEntityRights er2 = new()
            {
                EntityTypeName = nameof(TestChildObject),
                ReadQueryFilter = "OwnerObject.TextAttr.Contains(\"Real\")",
                CreateTLS = "",
                CreateOLS = "",
                ModifyTLS = "obj.OwnerObject.DecimalAttr = 10)",
                DeleteTLS = "obj.OwnerObject.DecimalAttr = 20",
                RoleId = roleUser!.Id
            };

            _appDbContext.Add(er2);
            await _appDbContext.SaveChangesAsync();
            
            TEntityRights er3 = new()
            {
                EntityTypeName = nameof(TestChildChildObject),
                ReadAttrs = "Name", // читать можем только Name
                ModifyAttrs = "Name, Comment", // изменять можем все, кроме Name и Comment
                CanModifyAttrs = false,
                RoleId = roleUser!.Id
            };

            _appDbContext.Add(er3);
            await _appDbContext.SaveChangesAsync();
            

            // Заново принудительно выполняем Login, чтобы перезачитались новые права
            await _fixture.LoginTestuserAsync();
        }

        public Task InitializeAsync()
        {
            return _fixture.OneTimeSetUpAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
