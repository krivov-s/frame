using FluentAssertions;
using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Test;
using Frame.Shared;
using TestObject = Frame.Domain.Entities.Test.TestObject;
namespace Frame.Tests.TestCore;
using Xunit;

[Collection("DisableParallelism")]
public class TestObjectStorage(FrameTestFixtureExt fixture) : IClassFixture<FrameTestFixtureExt>
{
    private readonly FrameTestFixtureExt _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    [Fact]
    public async Task TestAddAsync()
    {
        IObjectStorage objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        objectStorage.Add(new TestObject() { IntAttr = 1, TextAttr = "Text 1", DateTimeAttr = DateTime.UtcNow });
        objectStorage.Add(new TestObject() { IntAttr = 2, TextAttr = "Text 2", DateTimeAttr = DateTime.UtcNow  });
        objectStorage.Add(new TestObject() { IntAttr = 3, TextAttr = "Text 3", DateTimeAttr = DateTime.UtcNow  });

        Result res = await objectStorage.SaveChangesAsync();
        res.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task TestDelAsync()
    {
        IObjectStorage objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        objectStorage.Add(new TestObject() { IntAttr = 1, TextAttr = "Text 1", DateTimeAttr = DateTime.UtcNow });
        objectStorage.Add(new TestObject() { IntAttr = 2, TextAttr = "Text 2", DateTimeAttr = DateTime.UtcNow  });
        objectStorage.Add(new TestObject() { IntAttr = 3, TextAttr = "Text 3", DateTimeAttr = DateTime.UtcNow  });

        Result res = await objectStorage.SaveChangesAsync();
        res.IsError.Should().BeFalse();

        Result<List<TestObject>> resList = await objectStorage.GetListAsync<TestObject>(null, "");
        resList.IsErrorOrNull.Should().BeFalse();
        List<TestObject> lst = resList.Value!;
        int countOriginal = lst.Count;

        res = objectStorage.Delete(lst[1]);
        res.IsError.Should().BeFalse();
        
        res = await objectStorage.SaveChangesAsync();
        res.IsError.Should().BeFalse();
        
        resList = await objectStorage.GetListAsync<TestObject>(null, "");
        resList.IsErrorOrNull.Should().BeFalse();
        lst = resList.Value!;
        lst.Count.Should().Be(countOriginal - 1);        
    }
    
    [Fact]
    public async Task TestGetListAsync()
    {
        IObjectStorage objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        objectStorage.Add(new TestObject() { IntAttr = 10, TextAttr = "Text 1", DateTimeAttr = DateTime.UtcNow  });
        objectStorage.Add(new TestObject() { IntAttr = 11, TextAttr = "Text 2", DateTimeAttr = DateTime.UtcNow  });
        objectStorage.Add(new TestObject() { IntAttr = 12, TextAttr = "Text 3", DateTimeAttr = DateTime.UtcNow  });

        Result res = await objectStorage.SaveChangesAsync();
        res.IsError.Should().BeFalse();

        Result<List<BaseEntity>> resList = await objectStorage.GetBaseEntityListAsync<TestObject>();
        resList.IsError.Should().BeFalse();
        resList.Value.Should().NotBeNull();
        List<BaseEntity> list = resList.Value!;
        list.Count.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task TestObjAsync()
    {
        IObjectStorage objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        TestObject obj1 = new() { IntAttr = 100, TextAttr = "Text 100", DateTimeAttr = DateTime.UtcNow };
        objectStorage.Add(obj1);
        objectStorage.Add(new TestObject() { IntAttr = 110, TextAttr = "Text 200", DateTimeAttr = DateTime.UtcNow  });
        objectStorage.Add(new TestObject() { IntAttr = 120, TextAttr = "Text 300", DateTimeAttr = DateTime.UtcNow  });

        Result res = await objectStorage.SaveChangesAsync();
        res.IsError.Should().BeFalse(res.ErrorResult);

        obj1.Id.Should().BeGreaterThan(0);

        Result<TestObject> resOk100 = await objectStorage.GetObjAsync<TestObject>(obj1.Id);
        resOk100.IsError.Should().BeFalse(resOk100.ErrorResult);
        resOk100.Value.Should().NotBeNull();
        resOk100.Value!.IntAttr.Should().Be(100);
        
        Result<TestObject> resErr101 = await objectStorage.GetObjAsync<TestObject>(101);
        resErr101.IsError.Should().BeFalse();
        resErr101.Value.Should().BeNull();

        resOk100 = await objectStorage.GetObjAsync<TestObject>(spec => spec.Where(f => f.IntAttr == 100));
        resOk100.IsError.Should().BeFalse(resOk100.ErrorResult);
        resOk100.Value.Should().NotBeNull();
        resOk100.Value!.IntAttr.Should().Be(100);

        // Более 1 объекта в результате - ошибка (ожидали 1, а там много)
        Result<TestObject> resErrText = await objectStorage.GetObjAsync<TestObject>(spec => spec.Where(f => f.TextAttr.Contains("Text")));
        resErrText.IsError.Should().BeTrue();
        
        // 0 объектов в результате - норм (попытались, не нашли)
        Result<TestObject> resErrFilter0 = await objectStorage.GetObjAsync<TestObject>(spec => spec.Where(f => f.IntAttr == 999999));
        resErrFilter0.IsError.Should().BeFalse(resErrFilter0.ErrorResult);
        resErrFilter0.Value.Should().BeNull();
    }

    [Fact]
    public async Task TestQuerySpecAsync()
    {
        // 1. Подготовка исходных данных
        IObjectStorage objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        TestObject oMain1 = new () { IntAttr = 888, TextAttr = "Text 888", DateTimeAttr = DateTime.UtcNow };
        objectStorage.Add(oMain1);

        TestObject oMain2 = new () { IntAttr = 999, TextAttr = "Text 999", DateTimeAttr = DateTime.UtcNow };
        objectStorage.Add(oMain2);

        TestChildObject oChild1 = new() { Name = "Child1", OwnerObject = oMain1 };
        objectStorage.Add(oChild1);
        
        TestChildObject oChild2 = new() { Name = "Child2", OwnerObject = oMain2, TreeParent = oChild1 };
        objectStorage.Add(oChild2);

        TestChildChildObject oChildChild1 = new() { Name = "Child2_child1", Comment = "Comment 1", OwnerChild = oChild2 };
        objectStorage.Add(oChildChild1);

        TestChildChildObject oChildChild2 = new() { Name = "Child2_child2", Comment = "Comment 2", OwnerChild = oChild2 };
        objectStorage.Add(oChildChild2);
        
        Result rSave = await objectStorage.SaveChangesAsync();
        rSave.IsError.Should().BeFalse(rSave.ErrorResult);
        
        // 2. Запрос с нулевым QuerySpecification
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();
        
        Result<List<TestChildObject>> resChilds = await objectStorage.GetListAsync<TestChildObject>(querySpec: null);
        resChilds.IsError.Should().BeFalse(resChilds.ErrorResult);
        resChilds.Value.Should().NotBeNull();
        resChilds.Value!.Count.Should().Be(2);
        
        // 3. Запрос с простым Where
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        Result<List<TestChildObject>> resChild1 = await objectStorage.GetListAsync<TestChildObject>(spec => 
            spec.Where(c => c.Id == oChild2.Id));
        resChild1.IsError.Should().BeFalse(resChild1.ErrorResult);
        resChild1.Value.Should().NotBeNull();
        resChild1.Value!.Count.Should().Be(1);
        resChild1.Value[0].Name.Should().Be("Child2");
        resChild1.Value[0].OwnerObject.Should().BeNull();
        resChild1.Value[0].TreeParent.Should().BeNull();
        
        // 4. Запрос с Where и Include
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        Result<List<TestChildObject>> resChild2 = await objectStorage.GetListAsync<TestChildObject>(
            spec2 => spec2  
                .Include(o => o.OwnerObject)
                .Include(o => o.TreeParent)
                .Include(o => o.TreeParent!.OwnerObject)
                .Where(c => c.Id == oChild2.Id));
        resChild2.IsError.Should().BeFalse(resChild2.ErrorResult);
        resChild2.Value.Should().NotBeNull();
        resChild2.Value!.Count.Should().Be(1);
        resChild2.Value[0].Name.Should().Be("Child2");
        resChild2.Value[0].OwnerObject.Should().NotBeNull();
        resChild2.Value[0].OwnerObject!.IntAttr.Should().Be(999);
        resChild2.Value[0].TreeParent.Should().NotBeNull();
        resChild2.Value[0].TreeParent!.Name.Should().Be("Child1");
        resChild2.Value[0].TreeParent!.OwnerObject.Should().NotBeNull();
        resChild2.Value[0].TreeParent!.OwnerObject!.IntAttr.Should().Be(888);
        
        // 4.1. Запрос с Include и OrderBy (режим FluentApi)
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        Result<List<TestChildObject>> res3 = await objectStorage.GetListAsync<TestChildObject>(spec3 => 
            spec3.Include(o => o.OwnerObject)
                .Include(o => o.TreeParent)
                .Include(o => o.TreeParent!.OwnerObject)
                .OrderByDesc(o => o.Name));
        res3.IsError.Should().BeFalse(res3.ErrorResult);
        res3.Value.Should().NotBeNull();
        res3.Value!.Count.Should().Be(2);
        res3.Value[0].Name.Should().Be("Child2");
        res3.Value[1].Name.Should().Be("Child1");

        // 4.2. Запрос с Include и OrderBy (режим с текстовыми Include)
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        Result<List<TestChildObject>> res4_2 = await objectStorage.GetListAsync<TestChildObject>(spec3 =>
            spec3.Include("OwnerObject").Build());
        res4_2.IsError.Should().BeFalse(res4_2.ErrorResult);
        
        res4_2 = await objectStorage.GetListAsync<TestChildObject>(spec3 =>
            spec3.Include("TreeParent.OwnerObject").Build());
        res4_2.IsError.Should().BeFalse(res4_2.ErrorResult);        

        res4_2 = await objectStorage.GetListAsync<TestChildObject>(spec3 =>
            spec3.Include<TestObject>("OwnerObject").Build());
        res4_2.IsError.Should().BeFalse(res4_2.ErrorResult);

        res4_2 = await objectStorage.GetListAsync<TestChildObject>(spec3 =>
            spec3.Include<TestObject>("TreeParent.OwnerObject").Build());
        res4_2.IsError.Should().BeFalse(res4_2.ErrorResult);
        
        res4_2 = await objectStorage.GetListAsync<TestChildObject>(spec3 => 
            spec3.Include("OwnerObject")
                .Include("TreeParent")
                .Include("TreeParent.OwnerObject")
                .OrderByDesc(o => o.Name));
        res4_2.IsError.Should().BeFalse(res4_2.ErrorResult);
        res4_2.Value.Should().NotBeNull();
        res4_2.Value!.Count.Should().Be(2);
        res4_2.Value[0].Name.Should().Be("Child2");
        res4_2.Value[1].Name.Should().Be("Child1");

        // 5. Запрос со строковым фильтром
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();
        
        Result<List<TestObject>> res4 = await objectStorage.GetListAsync<TestObject>(
            spec4 => spec4.Where("IntAttr == 999"));
        res4.IsError.Should().BeFalse(res4.ErrorResult);
        res4.Value.Should().NotBeNull();
        res4.Value!.Count.Should().Be(1);
        res4.Value[0].IntAttr.Should().Be(999);
        
        // 6. Запрос с вложенным множеством
        objectStorage = _fixture.CreateObjectStorage();
        objectStorage.Should().NotBeNull();

        Result<List<TestObject>> res6 = await objectStorage.GetListAsync<TestObject>(spec =>
            spec.Include(x => x.ChildObjects)
                .ThenInclude(x => x.TreeParent)
                .Include(x => x.ChildObjects)
                .ThenInclude(x => x.ChildChildObjects)
                .Where(x => x.IntAttr == 999));
        res6.IsError.Should().BeFalse(res6.ErrorResult);
        res6.Value.Should().NotBeNull();
        res6.Value!.Count.Should().Be(1);
        TestObject oo6 =  res6.Value[0];
        oo6.IntAttr.Should().Be(999);
        oo6.ChildObjects.Should().HaveCount(1);
        TestChildObject ooChild = oo6.ChildObjects[0];
        ooChild.Name.Should().Be("Child2");
        ooChild.ChildChildObjects.Count.Should().Be(2);
        TestChildChildObject ooChildChild =  ooChild.ChildChildObjects[0];
        ooChildChild.Name.Should().Be("Child2_child1");
        
        // 7. Запрос по вычислимым
        // В реализации через EF Core - не работает, нужно явно выполнять фильтрацию на клиенте
        // по-умолчанию в EF Core фильтрация выполняется на сервере БД
        // objectStorage = _fixture.CreateObjectStorage();
        // objectStorage.Should().NotBeNull();
        //
        // Result<List<TestObject>> res7 = await objectStorage.GetListAsync<TestObject>(spec =>
        //     spec.Where(x => x.CalcAttrInt == 999));
        // res7.IsError.Should().BeFalse(res6.ErrorResult);
        // res7.Value.Should().NotBeNull();
        // res7.Value!.Count.Should().Be(1);
    }

}