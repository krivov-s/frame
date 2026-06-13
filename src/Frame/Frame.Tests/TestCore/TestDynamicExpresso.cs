using FluentAssertions;
using Frame.Domain.Entities.Test;
using DynamicExpresso;

namespace Frame.Tests.TestCore;

[Collection("DisableParallelism")]
public class TestDynamicExpresso
{
    [Fact]
    public void Test_GetSet_Success()
    {
        TestObject testObject = new()
        {
            IntAttr = 1,
            DecimalAttr = (decimal)9
        };

        string testExpression = "obj.IntAttr";

        var options = InterpreterOptions.Default | InterpreterOptions.LambdaExpressions;
        var interpreter = new Interpreter(options).SetVariable("obj", testObject);
        var result = interpreter.Eval(testExpression);
        result.Should().NotBeNull();
        result.Should().Be(1);
    }
    
}