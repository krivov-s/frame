namespace Frame.Domain.Entities.Core.Scheduler;


public class CronExpression : BaseEntity, IBaseGenericEntity<CronExpression>
{
    public override string Description => Name;

    /// <summary>
    /// Краткое наименование 
    /// </summary>
    public string Name { get; set; } = "";
        
    /// <summary>
    /// Выражение для Cron
    /// </summary>
    public string Expression { get; set; } = "";
    
    /// <summary>
    /// Примечание (комментарий) к выражению
    /// </summary>
    public string Comment { get; set; } = "";

    public List<Field<CronExpression>> GetFields() { return Meta.Fields; }

    public static class Meta
    {
        public static readonly string HumanName = "Выражение для Cron";
        public static readonly Field<CronExpression> Name = new()
        {
            Name = nameof(Name), 
            StringGet = x => x.Name, 
            StringSet = (x, val) => x.Name = val ?? "",
            Required = true, 
            MaxLength = 255, 
            HumanName = "Наименование"
        };
        public static readonly Field<CronExpression> Expression = new()
        {
            Name = nameof(Expression), 
            StringGet = x => x.Expression, 
            StringSet = (x, val) => x.Expression = val ?? "", 
            Required = true, 
            MaxLength = 40, 
            HumanName = "Выражение", 
            HelperText = "Cron-выражение [sec min hour day month dow]. Пример: 0 * * * * ? => каждую минуту"
        };
        public static readonly Field<CronExpression> Comment = new()
        {
            Name = nameof(Comment), 
            StringGet = x => x.Comment, 
            StringSet = (x, val) => x.Comment = val ?? "", 
            Required = false, 
            MaxLength = 2048, 
            HumanName = "Комментарий"
        };

        public static readonly List<Field<CronExpression>> Fields = [Name, Expression, Comment];
    }
}
