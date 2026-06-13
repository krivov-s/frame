using System.Linq.Expressions;

namespace Frame.Domain.QuerySpec;

/// <summary>
/// Спецификация запроса. Содержит в себе фильтры, сортировки, join-ы (секция Include), которые нужно применить к запросу.
/// </summary>
/// <typeparam name="TEntity">Тип объекта запроса</typeparam>
public interface IQuerySpecification<TEntity>
{
    /// <summary>
    /// Перечень связанных объектов, которые должны быть добавлены в запрос (секция Include, основа для Join).
    /// </summary>
    public List<Expression<Func<TEntity, object>>> Includes { get; }
    
    /// <summary>
    /// Фильтры в виде выражения Expression.
    /// </summary>
    public Expression<Func<TEntity, bool>>? Filter { get; }

    /// <summary>
    /// Фильтры в виде текстовой строки.
    /// </summary>
    public string FilterString { get; }
    
    /// <summary>
    /// Сортировки (секция OrderBy)
    /// </summary>
    public List<SortOrder<TEntity>> Orderings { get; }

    /// <summary>
    /// Сортировки (секция OrderBy) в виде текстовой строки.
    /// </summary>
    public string OrderingsString { get; }

    /// <summary>
    /// Ограничение по количеству возвращаемых записей. 0 - без ограничений.
    /// </summary>
    public int LimitRecords { get; }

    public List<IncludeChain> IncludeChains { get; }
    
    /// <summary>
    /// Метод добавления условия в фильтр. 
    /// Полностью заменяет значение <see cref="Filter"/> и затирает <see cref="FilterString"/>.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public IQuerySpecification<TEntity> Where(Expression<Func<TEntity, bool>> expression);
    
    /// <summary>
    /// Метод добавления условия в фильтр в виде текстовой строки.
    /// Полностью заменяет значение <see cref="FilterString"/> и затирает <see cref="Filter"/>.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public IQuerySpecification<TEntity> Where(string expression);

    /// <summary>
    /// <inheritdoc cref="LimitRecords"/>
    /// </summary>
    public IQuerySpecification<TEntity> Take(int limit);

    /// <summary>
    /// Метод добавления сортировки по возрастанию.  
    /// Добавляет значение в <see cref="Orderings"/> и затирает <see cref="OrderingsString"/>.
    /// </summary>
    /// <param name="keySelector">Поле для сортировки</param>
    /// <returns></returns>
    public IQuerySpecification<TEntity> OrderByAsc(Expression<Func<TEntity, object>> keySelector);
    
    /// <summary>
    /// Метод добавления сортировки по убыванию. 
    /// Добавляет значение в <see cref="Orderings"/> и затирает <see cref="OrderingsString"/>.
    /// </summary>
    /// <param name="keySelector">Поле для сортировки</param>
    /// <returns></returns>
    public IQuerySpecification<TEntity> OrderByDesc(Expression<Func<TEntity, object>> keySelector);
    
    IQuerySpecification<TEntity> ThenByAsc(Expression<Func<TEntity, object>> keySelector);
    IQuerySpecification<TEntity> ThenByDesc(Expression<Func<TEntity, object>> keySelector);

    
    /// <summary>
    /// Метод для добавления в запрос одиночного Join-а, простой связки, т.е. для reference-навигаций (1:1, многие:1 и т.д.)
    /// </summary>
    /// <param name="navigationPropertyPath"></param>
    /// <returns></returns>
    public IncludeBuilder<TEntity, TProperty> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> navigationPropertyPath);

    /// <summary>
    /// Метод для добавления в запрос одиночного Join-а, простой связки, т.е. для reference-навигаций (1:1, многие:1 и т.д.)
    /// </summary>
    /// <param name="navigationPropertyPath">Путь к ассоциации в виде строки, разделенной точками</param>
    /// <typeparam name="TProperty">Тип конечного свойства пути</typeparam>
    /// <returns></returns>
    public IncludeBuilder<TEntity, TProperty> Include<TProperty>(string navigationPropertyPath);

    /// <summary>
    /// Метод для добавления в запрос одиночного Join-а, простой связки, т.е. для reference-навигаций (1:1, многие:1 и т.д.)
    /// </summary>
    /// <param name="navigationPropertyPath">Путь к ассоциации в виде строки, разделенной точками</param>
    /// <returns></returns>
    public IncludeBuilder<TEntity, object> Include(string navigationPropertyPath);
    
    /// <summary>
    /// Метод для добавления в запрос коллекций (1:many, many:many)
    /// </summary>
    /// <param name="navigationPropertyPath"></param>
    /// <typeparam name="TElement"></typeparam>
    /// <returns></returns>
    IncludeBuilder<TEntity, TElement> Include<TElement>(
        Expression<Func<TEntity, List<TElement>>> navigationPropertyPath);    
    
}

/// <summary>
/// Служебный класс для хранения сведений о сортировках
/// </summary>
/// <typeparam name="TEntity">Тип объекта запроса</typeparam>
public class SortOrder<TEntity>
{
    /// <summary>
    /// Поле, по которому проводится сортировка
    /// </summary>
    public required Expression<Func<TEntity, object>> KeySelector { get; init; }
    
    /// <summary>
    /// Направление сортировки: false - по возрастанию, true - по убыванию.
    /// </summary>
    public required bool Descending { get; init; }
}
