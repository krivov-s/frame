using System.Text.Json;
using Frame.Shared;

namespace Frame.Domain;

public enum EWeekDay
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
};

/// <summary>
/// Класс представляет собой рабочий график на неделю с указанием рабочих дней, рабочего времени, кол-ва часов 
/// </summary>
public class WorkWeek
{
    public Dictionary<EWeekDay, WeekDay> WeekDays = new Dictionary<EWeekDay, WeekDay>()
    {
        { EWeekDay.Monday , new WeekDay() { DayOfWeek = EWeekDay.Monday } },
        { EWeekDay.Tuesday , new WeekDay() { DayOfWeek = EWeekDay.Tuesday } },
        { EWeekDay.Wednesday , new WeekDay() { DayOfWeek = EWeekDay.Wednesday } },
        { EWeekDay.Thursday , new WeekDay() { DayOfWeek = EWeekDay.Thursday } },
        { EWeekDay.Friday , new WeekDay() { DayOfWeek = EWeekDay.Friday } },
        { EWeekDay.Saturday , new WeekDay() { DayOfWeek = EWeekDay.Saturday, IsWorkDay = false } },
        { EWeekDay.Sunday , new WeekDay() { DayOfWeek = EWeekDay.Sunday, IsWorkDay = false } }
    };
    public WeekDay Monday => WeekDays[EWeekDay.Monday];
    public WeekDay Tuesday => WeekDays[EWeekDay.Tuesday];
    public WeekDay Wednesday => WeekDays[EWeekDay.Wednesday];
    public WeekDay Thursday => WeekDays[EWeekDay.Thursday];
    public WeekDay Friday => WeekDays[EWeekDay.Friday];
    public WeekDay Saturday => WeekDays[EWeekDay.Saturday];
    public WeekDay Sunday => WeekDays[EWeekDay.Sunday];
    
    public WeekDay this[EWeekDay weekDay]
    {
        get => WeekDays[weekDay];
        set => WeekDays[weekDay] = value;
    }

    public string GetDescription()
    {
        string descr = "";
        foreach (var kvp in WeekDays.Where(x => x.Value.IsWorkDay))
        {
            if(descr.Length > 0) descr += ", ";
            descr += kvp.Value.HumanNameShort;
        }
        return descr;
    }

    public void CopyTimeFromMonday()
    {
        foreach (var kvp in WeekDays.Where(x => x.Key != EWeekDay.Monday))
        {
            kvp.Value.Begin = WeekDays[EWeekDay.Monday].Begin;
            kvp.Value.End = WeekDays[EWeekDay.Monday].End;
        }
    }
    // public WeekDay Tuesday { get; set; } = new WeekDay();
    // public WeekDay Wednesday { get; set; } = new WeekDay();
    // public WeekDay Thursday { get; set; } = new WeekDay();
    // public WeekDay Friday { get; set; } = new WeekDay();
    // public WeekDay Saturday { get; set; } = new WeekDay();
    // public WeekDay Sunday { get; set; } = new WeekDay() { IsWorkDay = false };

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            IgnoreReadOnlyProperties = true
        };
        
        return JsonSerializer.Serialize(WeekDays, options);
    }

    public Result FromJson(string json)
    {
        try
        {
            Dictionary<EWeekDay, WeekDay>? dct =JsonSerializer.Deserialize<Dictionary<EWeekDay, WeekDay>>(json);
            if (dct == null)
            {
                return Result.Error("Ошибка десериализации WeekDays из json");
            }
            else
            {
                WeekDays = dct;
                return Result.Success;
            }
        }
        catch (Exception e)
        {
            return Result.Error("Ошибка десериализации WeekDays из json", e);
        }
    }
    
    public static Result<WorkWeek> CreateFromJson(string json)
    {
        WorkWeek week = new();
        Result result = week.FromJson(json);
        if (result.IsError)
        {
            return Result<WorkWeek>.Error(result.ErrorResult);
        }
        else
        {
            return Result<WorkWeek>.Success(week);
        }
    }
}

public class WeekDay
{
    public EWeekDay DayOfWeek { get; set; }
    public bool IsWorkDay { get; set; } = true;
    public TimeOnly Begin { get; set; } = new TimeOnly(9,0);
    public TimeOnly End { get; set; } = new TimeOnly(18,0);
    public TimeSpan WorkingTime => (End - Begin);

    public string HumanName => DayOfWeek switch
    {
        EWeekDay.Monday => "Понедельник",
        EWeekDay.Tuesday => "Вторник",
        EWeekDay.Wednesday => "Среда",
        EWeekDay.Thursday => "Четверг",
        EWeekDay.Friday => "Пятница",
        EWeekDay.Saturday => "Суббота",
        EWeekDay.Sunday => "Воскресенье",
        _ => "Ошибка! Неизвестный день недели!"
    };

    public string HumanNameShort => DayOfWeek switch
    {
        EWeekDay.Monday => "Пн",
        EWeekDay.Tuesday => "Вт",
        EWeekDay.Wednesday => "Ср",
        EWeekDay.Thursday => "Чт",
        EWeekDay.Friday => "Пт",
        EWeekDay.Saturday => "Сб",
        EWeekDay.Sunday => "Вс",
        _ => "Ошибка! Неизвестный день недели!"
    };
    
    // public string HumanName
    // {
    //     get
    //     {
    //         switch (DayOfWeek)
    //         {
    //             case EWeekDay.Monday => "Понедельник"; 
    //             
    //         }
    //     }
    // }

}