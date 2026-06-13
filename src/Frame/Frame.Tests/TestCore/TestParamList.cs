using FluentAssertions;
using Frame.App.IEntityRepositories;
using SmartFormat;
using Frame.Domain.Entities.Core.Security;
using Frame.Domain.Entities.Test;
using Frame.Domain.Entities.Core.Params;
using Frame.Domain.Params;
using Frame.Infrastructure.DBContext;
using Frame.Shared;
using Microsoft.EntityFrameworkCore;


namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestParamList : IClassFixture<FrameTestFixtureExt>
    {
        private readonly FrameTestFixtureExt _fixture;

        private AppDbContext _appDbContext;
        
        public TestParamList(FrameTestFixtureExt fixture)
        {            
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _appDbContext = _fixture.GetAppDbContext() ??
                            throw new Exception("Ошибка получения AppDbContext от FrameTestFixtureExt");
        }
        
        [Fact]
        public void CanSerializeAndDeserializeWithCorrectTypes()
        {
            var originalParamList = new ParamList();
            originalParamList.AddParam("StringParam", "TestString");
            originalParamList.AddParam("IntParam", 42);
            originalParamList.AddParam("LongParam", 9223372036854775807L); // Max long value
            originalParamList.AddParam("DoubleParam", 3.14);
            originalParamList.AddParam("BoolParam", true);
            originalParamList.AddParam("ListParam", new List<int> { 1, 2, 3 });
            originalParamList.AddParam("NestedParam", new Dictionary<string, object> { { "NestedKey", "NestedValue" } });
            originalParamList.AddParam("ObjParam", new TestObject {TextAttr= "Attr", DateTimeAttr= DateTime.UtcNow , IntAttr = 1, DecimalAttr = 10});

            string json = originalParamList.ToJson();

            ParamList deserializedPL = new();
            deserializedPL.FromJson(json);
            dynamic deserializedParamList = deserializedPL;

            Assert.Equal("TestString", deserializedParamList.StringParam);
            Assert.Equal("TestString", deserializedParamList["StringParam"]);
            Assert.IsType<string>(deserializedParamList.StringParam);

            Assert.Equal(42, deserializedParamList.IntParam);
            Assert.Equal(42, deserializedParamList["IntParam"]);
            Assert.IsType<int>(deserializedParamList.IntParam);

            Assert.Equal(9223372036854775807L, deserializedParamList.LongParam);
            Assert.Equal(9223372036854775807L, deserializedParamList["LongParam"]);
            Assert.IsType<long>(deserializedParamList.LongParam);

            Assert.Equal(3.14, deserializedParamList.DoubleParam);
            Assert.Equal(3.14, deserializedParamList["DoubleParam"]);
            Assert.IsType<double>(deserializedParamList.DoubleParam);

            Assert.True(deserializedParamList.BoolParam);
            Assert.IsType<bool>(deserializedParamList.BoolParam);

            Assert.IsType<List<int>>(deserializedParamList.ListParam);
            Assert.Equal(new List<int> { 1, 2, 3 }, deserializedParamList.ListParam);

            Assert.IsType<Dictionary<string, object>>(deserializedParamList.NestedParam);
            //Assert.Equal("NestedValue", ((Dictionary<string, object>)deserializedParamList.NestedParam)["NestedKey"]); TODO: Не десериализует JSON с вложенным параметром

            string json1 = @"{
            ""Long name param"" : {
                ""TypeParam"" : ""System.Boolean, System.Private.CoreLib"",
                ""Value"" : true,
                ""IsHidden"" : false,
                ""HumanName"" : """"
                }
            }";
            ParamList pl1 = new ParamList();
            pl1.FromJson(json1);
            pl1["Long_name_param"].Should().NotBeNull();
            bool bVal = (bool)pl1["Long_name_param"]!;
            bVal.Should().BeTrue();

        }

        [Fact]
        public void CanUseParamListWithSmartFormat()
        {
            dynamic userParamList = new ParamList();
            userParamList.AddParam("IntParam", 99);
            userParamList.AddParam("BoolParam", true);

            dynamic systemParamList = new ParamList();
            systemParamList.AddParam("StringParam", "TestString");

            Assert.Equal(99, userParamList.IntParam);

            Division division = new Division() {Id = 1};
            userParamList.AddParam("Division", division);
            
            var container = new
            {
                UserParamList = userParamList.AsDynDictionary(),
                SystemParamList = systemParamList.AsDynDictionary()
            };

            string strFilterData = "User.Id = {UserParamList.IntParam}";
            strFilterData = Smart.Format(strFilterData, container);
            strFilterData.Should().Be("User.Id = 99");

            var x1 = userParamList["Division"];
            var x2 = userParamList["Division"].Id;
            strFilterData = "Id = {UserParamList.Division.Id}";
            strFilterData = Smart.Format(strFilterData, container);
            strFilterData.Should().Be("Id = 1");
            
            strFilterData = "DocumentName = \"{SystemParamList.StringParam}\" and Id = {UserParamList.IntParam}";
            strFilterData = Smart.Format(strFilterData, container);
            strFilterData.Should().Be("DocumentName = \"TestString\" and Id = 99");
        }


        [Fact]
        public void CanAddAndRetrieveParameter()
        {
            dynamic paramList = new ParamList();
            paramList.TestParam = "TestValue";
            Assert.Equal("TestValue", paramList.TestParam);
            
            paramList["AnotherParam"] = "TestValue2";
            Assert.Equal("TestValue2", paramList["AnotherParam"]);
            
            paramList["IntParam"] = 25;
            Assert.Equal(25, paramList["IntParam"]);
        }

        [Fact]
        public void CanAddParamUsingGenericMethod()
        {
            var paramList = new ParamList();
            paramList.AddParam("IntParam", 42);
            paramList.AddParam("StringParam", "TestString");
            paramList.AddParam("BoolParam", true);

            dynamic dynamicParamList = paramList;
            Assert.Equal(42, dynamicParamList.IntParam);
            Assert.Equal("TestString", dynamicParamList.StringParam);
            Assert.True(dynamicParamList.BoolParam);
        }

        [Fact]
        public void CanRemoveParameter()
        {
            dynamic paramList = new ParamList();
            paramList.TestParam = "TestValue";
            ((ParamList)paramList).RemoveParameter("TestParam");
            try
            {
                var value = paramList.TestParam;
                Assert.Fail("Параметр существует, а должен был быть удален!!!");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail("Неожиданное исключение: " + ex.Message);
            }
        }

        [Fact]
        public void CanSerializeAndDeserialize()
        {
            var originalParamList = new ParamList();
            originalParamList.AddParam("StringParam", "TestString");
            originalParamList.AddParam("IntParam", 42, true, "Целое число");
            originalParamList.AddParam("BoolParam", true);
            originalParamList.AddParam("Long name param", true);

            // Проверка на IsHidden
            Result<bool> rIsHidden = originalParamList.IsParameterHidden("IntParam");
            Assert.False(rIsHidden.IsError);
            Assert.True(rIsHidden.Value);
            
            rIsHidden = originalParamList.IsParameterHidden("BoolParam");
            Assert.False(rIsHidden.IsError);
            Assert.False(rIsHidden.Value);

            string json = originalParamList.ToJson();
            dynamic deserializedParamList = ParamList.CreateFromJson(json);

            Assert.Equal("TestString", deserializedParamList.StringParam);
            Assert.Equal(42, deserializedParamList.IntParam);
            Assert.True(deserializedParamList.BoolParam);
            
            // Проверка на IsHidden
            rIsHidden = deserializedParamList.IsParameterHidden("IntParam");
            Assert.False(rIsHidden.IsError);
            Assert.True(rIsHidden.Value);
            
            // Проверка на HumanName
            ParamList? pl = deserializedParamList as ParamList;
            pl.Should().NotBeNull();
            ParamData? pv = pl!.GetParamValue("IntParam");
            pv.Should().NotBeNull();
            pv.HumanName.Should().Be("Целое число");

            Assert.Equal(42, deserializedParamList.IntParam);
            
            rIsHidden = deserializedParamList.IsParameterHidden("BoolParam");
            Assert.False(rIsHidden.IsError);
            Assert.False(rIsHidden.Value);
        }

        [Fact]
        public async Task CanBeStoredInParamListEntity()
        {
            // ParamList сконфигурирован, чтобы у пользователя всегда был только один ParamList
            // Автоматическое сохранение списка параметров при сохранении пользователя работает только
            // в полностью работающем сервере с функцирующим MessageBus, поскольку оно проходит через
            // отложенный асинхронный Notification
            IObjectStorage stg = _fixture.CreateObjectStorage();
            User user = new() { Login = "zzz", NormalizedLogin = "ZZZ" };
            user.SetPassword("ZZZ");
            stg.Add(user);
            await stg.SaveChangesAsync();

            List<UserProfile> list = await _appDbContext.GetSet<UserProfile>().Where(pl => pl.UserId == user.Id).ToListAsync();
            list.Count.Should().Be(0);

            UserProfile userProfile = new();
            var originalParamList = new ParamList();
            originalParamList.AddParam("StringParam", "TestString");
            originalParamList.AddParam("IntParam", 42);
            originalParamList.AddParam("BoolParam", true);

            userProfile.UserId = user.Id;  
            userProfile.SetParamList(originalParamList);

            _appDbContext.Add(userProfile);

            await _appDbContext.SaveChangesAsync();

            userProfile.Id.Should().BeGreaterThan(0);
            
            list = await _appDbContext.GetSet<UserProfile>().Where(pl => pl.UserId == user.Id).ToListAsync();
            list.Count.Should().Be(1);

            UserProfile ? entity2 = await _appDbContext.GetSet<UserProfile>().Where(pl => pl.Id == userProfile.Id).FirstOrDefaultAsync();

            entity2.Should().NotBeNull();
            dynamic pl = entity2!.GetParamList();
            Assert.NotNull(pl);
            Assert.Equal("TestString", pl.StringParam);
            Assert.Equal(42, pl.IntParam);
        }

        [Fact]
        public void TestGetParamByName()
        {
            var pl = new ParamList();
            pl.AddParam("StringParam", "TestString");
            pl.AddParam("IntParam", 42);
            pl.AddParam("BoolParam", true);

            bool bNotExists = pl.IsParameterExists("NotExistingParam");
            bNotExists.Should().BeFalse();
            bool bExists = pl.IsParameterExists("BoolParam");
            bExists.Should().BeTrue();

            Result<object?> res = pl.GetByName("StringParam");
            res.IsError.Should().BeFalse(res.ErrorResult);
            res.Value.Should().NotBeNull();
            string? strVal = res!.Value!.ToString();
            strVal.Should().Be("TestString");

            Result<object?> resFailed = pl.GetByName("NotExistingParam");
            resFailed.IsError.Should().BeTrue();
        }

        [Fact]
        public void TestGetParamByT()
        {
            DateTime dt = new DateTime(2025, 12, 24, 22, 00, 01, DateTimeKind.Utc);
            DateOnly d = new DateOnly(2030, 12, 18);
            var pl = new ParamList();
            pl.AddParam("StringParam", "TestString");
            pl.AddParam("IntParam", 42);
            pl.AddParam("BoolParam", true);
            pl.AddParam("DateTimeParam", dt);
            pl.AddParam("DateOnlyParam", d);

            string? s = pl.GetParamValue<string>("StringParam", "");
            s.Should().NotBeNull();
            s.Should().Be("TestString");

            int? i = pl.GetParamValue<int>("IntParam", 0);
            i.Should().NotBeNull();
            i.Should().Be(42);

            bool? b = pl.GetParamValue<bool>("BoolParam", false);
            b.Should().NotBeNull();
            b.Should().Be(true);

            DateTime? dt1 = pl.GetParamValue<DateTime>("DateTimeParam", DateTime.MinValue);
            dt1.Should().NotBeNull();
            dt1.Should().Be(dt);

            DateOnly? d1 = pl.GetParamValue<DateOnly>("DateOnlyParam", DateOnly.MinValue);
            d1.Should().NotBeNull();
            d1.Should().Be(d);

            d1 = pl.GetParamValue<DateOnly>("DateTimeParam", DateOnly.MinValue);
            d1.Should().NotBeNull();
            d1.Should().Be(DateOnly.FromDateTime(dt));
        }
        
        [Fact]
        public void TestSetParamByName()
        {
            var pl = new ParamList();
            pl.AddParam("StringParam", "TestString");
            Result<object?> res = pl.GetByName("StringParam");
            res.IsError.Should().BeFalse(res.ErrorResult);
            res.Value.Should().Be("TestString");

            pl.AddParam("IntParam", 25);
            res = pl.GetByName("IntParam");
            res.IsError.Should().BeFalse(res.ErrorResult);
            // Какая-то херь у компилятора с null-ами.
            if(res != null && res.Value != null)
            {
                string? strVal = res.Value.ToString();
                if (strVal != null) 
                {
                    int intVal = int.Parse(strVal);
                    intVal.Should().Be(25);
                }
                else
                {
                    Assert.Fail("strVal == null");
                }
            }
            else
            {
                Assert.Fail("res == null or res.Value == null");
            }

            dynamic dpl = pl;
            int i = dpl.IntParam;
            i.Should().Be(25);
        }

        [Fact]
        public void TestOperatorAdd()
        {
            var pl1 = new ParamList();
            pl1.AddParam("p1", "TestString");
            pl1.AddParam("p2", 25);
            var pl2 = new ParamList();
            pl2.AddParam("p3", "p3");
            pl1.AddParam("p2", 50);
            
            ParamList pl3 = pl1 + pl2;
            pl3.CountAll.Should().Be(3);
            pl3.GetByName("p1").Value.Should().Be("TestString");
            pl3.GetByName("p2").Value.Should().Be(50);
            pl3.GetByName("p3").Value.Should().Be("p3");
        }

        [Fact]
        public void TestIterateParamList()
        {
            var pl1 = new ParamList();
            pl1.AddParam("p1", 1);
            pl1.AddParam("p2", 2);
            pl1.AddParam("p3", 3);
            pl1.AddParam("p4", 4);

            int i = 0;
            foreach (var paramValue in pl1.Values)
            {
                i++;
                string etalonName = $"p{i}";
                // Проверка значений из итератора
                string name = paramValue.Key;
                name.Should().Be(etalonName);
                object? value = paramValue.Value;
                value.Should().NotBeNull();
                value.Should().Be(i);
                
                // Проверка значения из индексатора ParamList[ParamName]
                object? value2 = pl1[etalonName];
                value2.Should().NotBeNull();
                value2.Should().Be(i);
            }
        }
    }

    public class TestParams
    {
        public int TestId = 1;
        public Dictionary<string, object> ParamList = [];
    }

    static class TestParamsProvider
    {
        public static TestParams GetParams()
        {
            TestParams prms = new TestParams();
            prms.ParamList.Add("CurrentDate", "01.01.2024");
            return prms;
        }
    }
}

