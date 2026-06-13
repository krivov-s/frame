using System.Linq.Expressions;

namespace Frame.Domain.QuerySpec;


/// <summary>
/// Контракт для контейнеров, которые хранят IncludeChains.
/// Сделан не-дженериковым, чтобы IncludeBuilder не зависел от конкретного типа спецификации.
/// </summary>
public interface IIncludeChainContainer
{
    List<IncludeChain> IncludeChains { get; }
}

/// <summary>
/// Цепочка шагов Include/ThenInclude
/// </summary>
public class IncludeChain
{
    public List<IncludeStep> Steps { get; } = new List<IncludeStep>();
}

/// <summary>
/// Шаг цепочки Include/ThenInclude
/// </summary>
public class IncludeStep(LambdaExpression expression, Type previousType, Type propertyType)
{
    public LambdaExpression Expression { get; } = expression;
    public Type PreviousType { get; } = previousType;
    public Type PropertyType { get; } = propertyType;
}
