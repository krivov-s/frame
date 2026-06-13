using Frame.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using Frame.Domain.QuerySpec;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Frame.Infrastructure;

/// <summary>
/// Набор вспомогательных сервисных функций для работы с EF.Core
/// </summary>
public static class EFHelpers
{
    /// <summary>
    /// Метод применяет спецификацию запроса <paramref name="querySpec"/> к запросу <paramref name="query"/>
    /// </summary>
    public static IQueryable<TEntity> ApplyQuerySpec<TEntity>(
        IQuerySpecification<TEntity>? querySpec,
        IQueryable<TEntity> query)
        where TEntity : BaseEntity, IBaseGenericEntity<TEntity>, new()
    {
        if (querySpec == null)
            return query;

        // Одиночные Includes
        foreach (var include in querySpec.Includes)
            query = query.Include(include);

        // Цепочки Include -> ThenInclude
        foreach (var chain in querySpec.IncludeChains)
        {
            if (chain.Steps.Count == 0)
                continue;

            // Первый шаг
            var firstStep = chain.Steps[0];

            var includeMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == "Include" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), firstStep.PropertyType);

            var includable = includeMethod.Invoke(null, new object[] { query, firstStep.Expression })!;

            // ThenInclude шаги
            for (int i = 1; i < chain.Steps.Count; i++)
            {
                var prev = chain.Steps[i - 1];
                var current = chain.Steps[i];

                var prevType = prev.PropertyType;
                var currentType = current.PropertyType;

                var isCollection = prevType != typeof(string)
                                   && typeof(System.Collections.IEnumerable).IsAssignableFrom(prevType);

                var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == "ThenInclude" && m.GetParameters().Length == 2
                                && m.GetParameters()[0].ParameterType.Name.Contains("IIncludableQueryable"));

                if (isCollection && prevType.IsGenericType)
                {
                    var elementType = prevType.GetGenericArguments().First();
                    thenIncludeMethod = thenIncludeMethod.MakeGenericMethod(typeof(TEntity), elementType, currentType);
                }
                else
                {
                    thenIncludeMethod = thenIncludeMethod.MakeGenericMethod(typeof(TEntity), prevType, currentType);
                }

                includable = thenIncludeMethod.Invoke(null, new object[] { includable!, current.Expression })!;
            }

            query = (IQueryable<TEntity>)includable;
        }

        // Filter
        if (querySpec.Filter is not null)
            query = query.Where(querySpec.Filter);
        else if (!string.IsNullOrWhiteSpace(querySpec.FilterString))
            query = query.Where(querySpec.FilterString);

        // OrderBy
        if (querySpec.Orderings.Any())
        {
            foreach (var ordering in querySpec.Orderings)
            {
                query = ordering.Descending
                    ? query.OrderByDescending(ordering.KeySelector)
                    : query.OrderBy(ordering.KeySelector);
            }
        }
        else if (!string.IsNullOrWhiteSpace(querySpec.OrderingsString))
        {
            query = query.OrderBy(querySpec.OrderingsString);
        }

        if (querySpec.LimitRecords > 0)
            query = query.Take(querySpec.LimitRecords);

        // ВАЖНО: предотвращаем взрывной рост строк при вложенных коллекциях
        query = query.AsSplitQuery();

        return query;
    }

    public static ValueConverter<List<string>, string> GetStringListToJsonConverter()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        var converter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value, options),
            json => JsonSerializer.Deserialize<List<string>>(json, options) ?? new List<string>());

        return converter;
    }

    public static ValueComparer<List<string>> GetStringListToJsonComparer()
    {
        var comparer = new ValueComparer<List<string>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        return comparer;
    }
}
