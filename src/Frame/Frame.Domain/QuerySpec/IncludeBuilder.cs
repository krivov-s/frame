using System.Linq.Expressions;

namespace Frame.Domain.QuerySpec;

/// <summary>
/// IncludeBuilder: первый generic-параметр TRoot — всегда корневой тип запроса (TEntity).
/// Второй generic-параметр — тип предыдущего шага, для коллекций он будет элементом коллекции.
/// </summary>
public class IncludeBuilder<TRoot, TPreviousProperty>(QuerySpecification<TRoot> rootSpec, IncludeChain chain)
{
    private readonly QuerySpecification<TRoot> _rootSpec = rootSpec ?? throw new ArgumentNullException(nameof(rootSpec));
    private readonly IncludeChain _chain = chain ?? throw new ArgumentNullException(nameof(chain));

    // ThenInclude для reference навигации
    public IncludeBuilder<TRoot, TNext> ThenInclude<TNext>(
        Expression<Func<TPreviousProperty, TNext>> navigationPropertyPath)
    {
        _chain.Steps.Add(new IncludeStep(
            navigationPropertyPath,
            typeof(TPreviousProperty),
            navigationPropertyPath.ReturnType));

        return new IncludeBuilder<TRoot, TNext>(_rootSpec, _chain);
    }

    // ThenInclude для коллекции
    public IncludeBuilder<TRoot, TElement> ThenInclude<TElement>(
        Expression<Func<TPreviousProperty, List<TElement>>> navigationPropertyPath)
    {
        _chain.Steps.Add(new IncludeStep(
            navigationPropertyPath,
            typeof(TPreviousProperty),
            navigationPropertyPath.ReturnType));

        return new IncludeBuilder<TRoot, TElement>(_rootSpec, _chain);
    }

    /// <summary>
    /// Удобный переход к Include (на корне) прямо из IncludeBuilder — позволяет писать .ThenInclude(...).Include(...)
    /// Поддерживает обе перегрузки Include (reference и collection).
    /// </summary>
    public IncludeBuilder<TRoot, TNext> Include<TNext>(Expression<Func<TRoot, TNext>> navigationPropertyPath)
        => _rootSpec.Include(navigationPropertyPath);

    public IncludeBuilder<TRoot, TNext> Include<TNext>(string navigationPropertyPath)
        => _rootSpec.Include<TNext>(navigationPropertyPath);

    public IncludeBuilder<TRoot, object> Include(string navigationPropertyPath)
        => _rootSpec.Include<object>(navigationPropertyPath);
    
    public IncludeBuilder<TRoot, TNext> Include<TNext>(Expression<Func<TRoot, List<TNext>>> navigationPropertyPath)
        => _rootSpec.Include(navigationPropertyPath);

    
    // Возврат к корневой спецификации
    public QuerySpecification<TRoot> Build() => _rootSpec;

    // Прокси-методы, чтобы можно было продолжить fluent
    public QuerySpecification<TRoot> Where(Expression<Func<TRoot, bool>> expression)
    {
        _rootSpec.Where(expression);
        return _rootSpec;
    }

    public QuerySpecification<TRoot> Take(int limit)
    {
        _rootSpec.Take(limit);
        return _rootSpec;
    }

    public QuerySpecification<TRoot> OrderByAsc(Expression<Func<TRoot, object>> keySelector)
    {
        _rootSpec.OrderByAsc(keySelector);
        return _rootSpec;
    }

    public QuerySpecification<TRoot> OrderByDesc(Expression<Func<TRoot, object>> keySelector)
    {
        _rootSpec.OrderByDesc(keySelector);
        return _rootSpec;
    }
}

