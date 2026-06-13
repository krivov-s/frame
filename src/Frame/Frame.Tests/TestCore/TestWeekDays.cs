using FluentAssertions;
using Frame.Domain;

namespace Frame.Tests.TestCore;

[Collection("DisableParallelism")]
public class TestWeekDays
{
    [Fact]
    public void TestSerialize()
    {
        WorkWeek wds = new();
        wds[EWeekDay.Monday].IsWorkDay = false;
        wds[EWeekDay.Tuesday].IsWorkDay = false;
        wds[EWeekDay.Wednesday].Begin = new TimeOnly(12, 00); 
        wds[EWeekDay.Wednesday].End = new TimeOnly(14, 00);
        
        string json = wds.ToJson();
        
        WorkWeek? wds1 = WorkWeek.CreateFromJson(json);
        wds1.Should().NotBeNull();
        wds1!.Monday.IsWorkDay.Should().BeFalse();
        wds1[EWeekDay.Monday].IsWorkDay.Should().BeFalse();
        wds1[EWeekDay.Monday].HumanName.Should().Be("Понедельник");

        wds1[EWeekDay.Wednesday].WorkingTime.Hours.Should().Be(2);
        
        wds1.Tuesday.IsWorkDay.Should().BeFalse();
        wds1[EWeekDay.Tuesday].IsWorkDay.Should().BeFalse();
        
        wds1.Wednesday.IsWorkDay.Should().BeTrue();
        wds1.Wednesday.Begin.Should().Be(new TimeOnly(12,00));
        wds1.Wednesday.End.Should().Be(new TimeOnly(14,00));
        wds1[EWeekDay.Wednesday].End.Should().Be(new TimeOnly(14,00));
    }
}