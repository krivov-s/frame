using FluentAssertions;
using Frame.Domain.Entities.Core.Scripting;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestObject = Frame.Domain.Entities.Test.TestObject;
using Frame.App.IEntityLoaders;
using Frame.Domain;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestScripting : IClassFixture<FrameTestFixtureExt>, IAsyncLifetime
    {
        private readonly FrameTestFixtureExt _fixture;

        private IScriptCore? _scriptCore;
        private async Task<IScriptCore> getScriptCoreAsync()
        {
            if (_scriptCore != null)
            {
                return _scriptCore;
            }
            _scriptCore = _fixture.ServiceProvider.GetService<IScriptCore>();
            _scriptCore.Should().NotBeNull();
    
            IEntityScriptLoader? esLoader = _fixture.ServiceProvider.GetService<IEntityScriptLoader>();
            esLoader.Should().NotBeNull();

            Result res = await _scriptCore!.InitializeAsync(esLoader!);
            res.IsError.Should().BeFalse(res.ErrorResult);
            
            return _scriptCore;
        }

        public TestScripting(FrameTestFixtureExt fixture) /*: base(fixture)*/
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _fixture.OnOneTimeSetUpAsync = OneTimeDbInitialize;
        }

        [Fact]
        public void Get_Hooks_SelectListItems()
        {
            ESelectListItems.HookTypes.Count.Should().Be(4);
            ESelectListItems.HookTypes[0].Value.Should().Be((int)EHookType.OnBeforeSave);
            ESelectListItems.HookTypes[3].Description.Should().Be(Enum.GetName(EHookType.OnAfterDelete));
        }

        [Fact]
        public async Task ScriptCore_OnBeforeSave_Sync_Ret_Success()
        {
            //TestFixture fixture = await InitFixtureDatabase();
            //IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
            //scriptCore.Should().NotBeNull();
            //Result res = await scriptCore!.InitializeAsync();
            //res.IsError.Should().BeFalse(res.ErrorResult);

            TestObject testObject = new()
            {
                IntAttr = 1,
                DecimalAttr = (decimal)9
            };

            IScriptCore scriptCore = await getScriptCoreAsync();
            Result res = await scriptCore.ExecuteEntityScriptAsync(testObject, EHookType.OnBeforeSave, EThreadType.Sync);
            res.IsError.Should().BeFalse(res.ErrorResult);
            testObject.TextAttr.Should().Be("10");
        }

        [Fact]
        public async Task ScriptCore_OnAfterSave_Async_Ret_Success()
        {
            //TestFixture fixture = await InitFixtureDatabase();
            //IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
            //scriptCore.Should().NotBeNull();
            //Result res = await scriptCore!.InitializeAsync();
            //res.IsError.Should().BeFalse(res.ErrorResult);

            TestObject testObject = new()
            {
                IntAttr = 1,
                DecimalAttr = (decimal)9
            };

            IScriptCore scriptCore = await getScriptCoreAsync();
            Result res = await scriptCore.ExecuteEntityScriptAsync(testObject, EHookType.OnAfterSave, EThreadType.Async);
            res.IsError.Should().BeFalse(res.ErrorResult);
            testObject.TextAttr.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ScriptCore_ScriptCommand_Execute_Ret_Success()
        {
            //TestFixture fixture = await InitFixtureDatabase();
            //IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
            //scriptCore.Should().NotBeNull();
            //Result res = await scriptCore!.InitializeAsync();
            //res.IsError.Should().BeFalse(res.ErrorResult);

            IObjectStorage? storage = _fixture.ServiceProvider.GetService<IObjectStorage>();
            storage.Should().NotBeNull();
            Result<IBaseRepository<ScriptCommand>> resRepo = storage!.GetBaseRepository<ScriptCommand>();
            resRepo.IsError.Should().BeFalse();
            resRepo.Value.Should().NotBeNull();

            Result<IQueryable<ScriptCommand>> resQuery = resRepo.Value!.GetQuerable();
            resQuery.IsError.Should().BeFalse();
            resQuery.Value.Should().NotBeNull();

            ScriptCommand? cmdSync = await resQuery.Value!.Where(sc => sc.ScriptName == "SimpleSync").FirstOrDefaultAsync();
            cmdSync.Should().NotBeNull();

            IScriptCore scriptCore = await getScriptCoreAsync();
            Result res = await scriptCore.ExecuteScriptCommandAsync(cmdSync!.Id);
            res.IsError.Should().BeFalse(res.ErrorResult);

            ScriptCommand? cmdAsync = await resQuery.Value!.Where(sc => sc.ScriptName == "SimpleAsync").FirstOrDefaultAsync();
            cmdAsync.Should().NotBeNull();

            res = await scriptCore.ExecuteScriptCommandAsync(cmdAsync!.Id);
            res.IsError.Should().BeFalse(res.ErrorResult);
        }

        [Fact]
        public async Task ScriptCore_ScriptCommand_Remove_Ret_Success()
        {
            //TestFixture fixture = await InitFixtureDatabase();
            //IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
            //scriptCore.Should().NotBeNull();
            //Result res = await scriptCore!.InitializeAsync();
            //res.IsError.Should().BeFalse(res.ErrorResult);

            IObjectStorage? storage = _fixture.ServiceProvider.GetService<IObjectStorage>();
            storage.Should().NotBeNull();
            Result<IBaseRepository<ScriptCommand>> resRepo = storage!.GetBaseRepository<ScriptCommand>();
            resRepo.IsError.Should().BeFalse();
            resRepo.Value.Should().NotBeNull();

            Result<IQueryable<ScriptCommand>> resQuery = resRepo.Value!.GetQuerable();
            resQuery.IsError.Should().BeFalse();
            resQuery.Value.Should().NotBeNull();

            ScriptCommand? cmd = await resQuery.Value!.Where(sc => sc.ScriptName == "SimpleAsync3").FirstOrDefaultAsync();
            cmd.Should().NotBeNull();

            IScriptCore scriptCore = await getScriptCoreAsync();
            Result res = scriptCore.RemoveScriptCommand(cmd!.Id);
            res.IsError.Should().BeFalse(res.ErrorResult);

            // Повторное выполнение без предварительной регистрации - должно быть норм.
            res = await scriptCore.ExecuteScriptCommandAsync(cmd!.Id);
            res.IsError.Should().BeFalse("Выполнение без предварительной регистрации - должно быть норм (будет выполнена компиляция и сохранение в кэш)");

            res = await scriptCore.ExecuteScriptCommandAsync(cmd!.Id);
            res.IsError.Should().BeFalse("Повторное выполнение без предварительной регистрации - должно быть норм (выполнение предварительно скомпилированного из кэша).");
        }

        [Fact]
        public async Task ScriptCore_EntityScript_Remove_Ret_Success()
        {
            //TestFixture fixture = await InitFixtureDatabase();
            //IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
            //scriptCore.Should().NotBeNull();
            //Result res = await scriptCore!.InitializeAsync();
            //res.IsError.Should().BeFalse(res.ErrorResult);

            IObjectStorage? storage = _fixture.ServiceProvider.GetService<IObjectStorage>();
            storage.Should().NotBeNull();
            Result<IBaseRepository<EntityScript>> resRepo = storage!.GetBaseRepository<EntityScript>();
            resRepo.IsError.Should().BeFalse();
            resRepo.Value.Should().NotBeNull();

            Result<IQueryable<EntityScript>> resQuery = resRepo.Value!.GetQuerable();
            resQuery.IsError.Should().BeFalse();
            resQuery.Value.Should().NotBeNull();

            EntityScript? script = await resQuery.Value!.Where(sc => sc.Id == 1).FirstOrDefaultAsync();
            script.Should().NotBeNull();

            IScriptCore scriptCore = await getScriptCoreAsync();
            Result res = scriptCore.RemoveEntityScript(script!);
            res.IsError.Should().BeFalse();

            TestObject testObject = new()
            {
                IntAttr = 1,
                DecimalAttr = (decimal)9
            };

            res = await scriptCore.ExecuteEntityScriptAsync(testObject, EHookType.OnBeforeSave, EThreadType.Sync);
            res.IsError.Should().BeFalse();
            testObject.TextAttr.Should().BeEmpty();
        }

        //[Fact]
        //public async Task New_Modify_Delete()
        //{

        //}

        //[Fact]
        //public async Task ScriptCore_Init_Ret_Success()
        //{
        //    TestFixture fixture = await InitFixtureDatabase();
        //    IScriptCore? scriptCore = fixture.ServiceProvider.GetService<IScriptCore>();
        //    scriptCore.Should().NotBeNull();
        //    Result res = await scriptCore!.InitializeAsync();
        //    res.IsError.Should().BeFalse(res.ErrorResult);
        //}

        private async Task<FrameTestFixture> InitFixtureDatabase()
        {
//            TestFixture fixture = AsyncTestInMemoryBaseWithFixture.CreateAndInitFixtureDatabase();
            AppDbContext dbContext = _fixture.ServiceProvider.GetRequiredService<AppDbContext>();

            ScriptCommand cmd1 = new()
            {
                ScriptName = "SimpleSync",
                CodeType = ECodeType.SimpleMethod,
                ThreadType = EThreadType.Sync,
                ScriptCode = @"
    string strVal = ""Hello, scripting world"";
    return Result.Success;
"
            };

            ScriptCommand cmd2 = new()
            {
                ScriptName = "SimpleAsync",
                CodeType = ECodeType.SimpleMethod,
                ThreadType = EThreadType.Async,
                ScriptCode = @"
IObjectStorage stg = appCore.CreateObjectStorage();
if(stg != null)
{
    Result<IBaseRepository<TestObject>> resRepo = stg.GetBaseRepository<TestObject>();
    if(resRepo.IsError == false && resRepo.Value != null)
    {
        IBaseRepository<TestObject> repo = resRepo.Value;
        Result<List<TestObject>> resList = await repo.GetAllAsync();
        if(resList.IsError == false && resList.Value != null)
        {
            return Result.Success;
        }
    }
}
return Result.Error(""Ошибка"");
"
            };

            ScriptCommand cmd3 = new()
            {
                ScriptName = "SimpleAsync3",
                CodeType = ECodeType.SimpleMethod,
                ThreadType = EThreadType.Async,
                ScriptCode = @"
IObjectStorage stg = appCore.CreateObjectStorage();
if(stg != null)
{
    Result<IBaseRepository<TestObject>> resRepo = stg.GetBaseRepository<TestObject>();
    if(resRepo.IsError == false && resRepo.Value != null)
    {
        IBaseRepository<TestObject> repo = resRepo.Value;
        Result<List<TestObject>> resList = await repo.GetAllAsync();
        if(resList.IsError == false && resList.Value != null)
        {
            return Result.Success;
        }
    }
}
return Result.Error(""Ошибка"");
"
            };

            EntityScript hook1 = new()
            {
                EntityType = $"{nameof(TestObject)}",
                HookType = EHookType.OnBeforeSave,
                CodeType = ECodeType.SimpleMethod,
                ThreadType = EThreadType.Sync,
                ScriptCode =
@"TestObject? to = obj as TestObject;
if(to == null)
{
    return Result.Error(""Переданный объект не является экземпляром TestObject"");
}
string str = $""{to.IntAttr + to.DecimalAttr}"";
to.TextAttr = str;
return Result.Success;"
            };

            EntityScript hook2 = new()
            {
                EntityType = $"{nameof(TestObject)}",
                HookType = EHookType.OnAfterSave,
                CodeType = ECodeType.SimpleMethod,
                ThreadType = EThreadType.Async,
                ScriptCode =
@"TestObject? to = obj as TestObject;
if(to == null)
{
    return Result.Error(""Переданный объект не является экземпляром TestObject"");
}
IObjectStorage stg = appCore.CreateObjectStorage();
if(stg != null)
{
    Result<IBaseRepository<TestObject>> resRepo = stg.GetBaseRepository<TestObject>();
    if(resRepo.IsError == false && resRepo.Value != null)
    {
        IBaseRepository<TestObject> repo = resRepo.Value;
        Result<List<TestObject>> resList = await repo.GetAllAsync();
        if(resList.IsError == false && resList.Value != null)
        {
            to.TextAttr = $""{resList.Value.Count}"";
            return Result.Success;
        }
    }
}
return Result.Error(""Ошибка"");"
            };

            dbContext.Add(cmd1);
            dbContext.Add(cmd2);
            dbContext.Add(cmd3);
            dbContext.Add(hook1);
            dbContext.Add(hook2);

            await dbContext.SaveChangesAsync();
            return _fixture;
        }

        public Task InitializeAsync()
        {
            return _fixture.OneTimeSetUpAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
        
        private Task OneTimeDbInitialize()
        {
            return InitFixtureDatabase();
        }

    }
}
