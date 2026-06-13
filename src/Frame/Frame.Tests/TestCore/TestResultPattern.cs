using FluentAssertions;
using Frame.Shared;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestResultPattern
    {
        [Fact]
        public void Test()
        {
            int result1 = Method_ok_ret_result_1();
            result1.Should().Be(1);
            result1 = Method_ok_ret_int_1();
            result1.Should().Be(1);

            Result<int> result2 = Method_ok_ret_result_1();
            Assert.Equal(1, (int)result2);
            result2.IsError.Should().Be(false);

            // result2 = Method_ok_ret_int_1();
            // Assert.Equal(1, (int)result2);
            // result2.IsError.Should().Be(false);

            string? result3 = Method_ok_ret_string();
            result3.Should().Be("Отлично сработали");

            Result<string> result4 = Method_ok_ret_string();
            result4.IsError.Should().Be(false);
            result4.Value.Should().Be("Отлично сработали");
            ((string?)result4).Should().Be("Отлично сработали");

            int result5 = Method_fail_ret_0();
            result5.Should().Be(0);
            Result<int> result6 = Method_fail_ret_0();
            result6.IsError.Should().Be(true);
            result6.Value.Should().Be(0);
            result6.ErrorResult.Should().Be("Херня");

            string? result7 = Method_fail_ret_null();
            result7.Should().BeNull();
            Result<string?> result8 = Method_fail_ret_null();
            result8.Value.Should().BeNull();
            ((string?)result8).Should().BeNull();
            result8.ErrorResult.Should().Be("Херня");
        }

        [Fact]
        public void TestWithDynamic()
        {
            Result<dynamic> res1 = Method_ret_dynamic_from_int(18);
            res1.IsError.Should().BeFalse();
            Assert.Equal(18, res1.Value);
            
            // Result<dynamic> res2 = Method_ret_dynamic_from_dynamic(18);
            // res2.IsError.Should().BeFalse();
            // Assert.Equal(18, res2.Value);

            dynamic r3 = Method_ret_dynamic_from_dynamic(18);
            Assert.Equal(18, r3.Value);
            Assert.Equal("", r3.ErrorResult);
        }

        private static Result<dynamic> Method_ret_dynamic_from_int(int value)
        {
            var r = Result<dynamic>.Success(value);
            return r;
        }

        private static Result<dynamic> Method_ret_dynamic_from_dynamic(dynamic value)
        {
            var r = Result<dynamic>.Success(value);
            return r;
        }

        private static Result<dynamic> Method_ret_dynamic_error()
        {
            return Result<dynamic>.Error("ОШИБКА!");
        }
        
        private static int Method_ok_ret_int_1()
        {
            return 1;
        }

        private static Result<int> Method_ok_ret_result_1()
        {
            return Result<int>.Success(1);
        }
        private static Result<string> Method_ok_ret_string()
        {
            return Result<string>.Success("Отлично сработали");
        }

        private static Result<int> Method_fail_ret_0()
        {
            Result<int> result = Result<int>.Error("Херня", 0);
            return result;
        }
        private static Result<string?> Method_fail_ret_null()
        {
            Result<string?> result = Result<string?>.Error("Херня");
            return result;
        }
    }
}
