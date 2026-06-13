using System.Linq.Expressions;
using System.Reflection;
using Frame.Shared;

namespace Frame.Domain.QuerySpec;

    public class QuerySpecification<TEntity> : IQuerySpecification<TEntity>, IIncludeChainContainer
    {
        public QuerySpecification(
            IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
            Expression<Func<TEntity, bool>>? filter = null,
            List<SortOrder<TEntity>>? orderings = null,
            string filterString = "",
            string orderingsString = "",
            int limitRecords = 0)
        {
            if (filter != null && filterString != "")
            {
                throw new FrameException($"В конструкторе {nameof(QuerySpecification<TEntity>)} недопустимо одновременно " +
                                         $"указывать фильтр в виде строки и выражения!");
            }

            if (orderings is { Count: > 0 } && orderingsString != "")
            {
                throw new FrameException($"В конструкторе {nameof(QuerySpecification<TEntity>)} недопустимо одновременно " +
                                         $"указывать сортировку в виде строки и в виде списка!");
            }

            Includes = includes?.ToList() ?? new List<Expression<Func<TEntity, object>>>();
            Filter = filter;
            Orderings = orderings?.ToList() ?? new List<SortOrder<TEntity>>();
            FilterString = filterString;
            OrderingsString = orderingsString;
            LimitRecords = limitRecords;
        }

        public List<Expression<Func<TEntity, object>>> Includes { get; }
        public Expression<Func<TEntity, bool>>? Filter { get; private set; }
        public string FilterString { get; private set; }
        public List<SortOrder<TEntity>> Orderings { get; }
        public string OrderingsString { get; private set; }
        public int LimitRecords { get; private set; }

        // Реализация IIncludeChainContainer
        public List<IncludeChain> IncludeChains { get; } = new List<IncludeChain>();

        public IQuerySpecification<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        {
            FilterString = "";

            if (Filter == null)
            {
                Filter = expression;
            }
            else
            {
                var parameter = Expression.Parameter(typeof(TEntity), "x");

                var left = Expression.Invoke(Filter, parameter);
                var right = Expression.Invoke(expression, parameter);
                var combined = Expression.AndAlso(left, right);

                Filter = Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
            }

            return this;
        }

        public IQuerySpecification<TEntity> Where(string expression)
        {
            FilterString = expression;
            Filter = null;
            return this;
        }

        public IQuerySpecification<TEntity> Take(int limit)
        {
            LimitRecords = limit;
            return this;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IncludeBuilder<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        {
            var chain = new IncludeChain();
            chain.Steps.Add(new IncludeStep(navigationPropertyPath, typeof(TEntity), navigationPropertyPath.ReturnType));
            IncludeChains.Add(chain);
            return new IncludeBuilder<TEntity, TProperty>(this, chain);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IncludeBuilder<TEntity, TProperty> Include<TProperty>(string navigationPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(navigationPropertyPath))
                throw new ArgumentException("Передан пустой путь, построение QuerySpecification невозможно", nameof(navigationPropertyPath));

            var parts = navigationPropertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw new ArgumentException("Передан некорректный путь, построение QuerySpecification невозможно", nameof(navigationPropertyPath));

            Type currentType = typeof(TEntity);
            ParameterExpression? parameter = null;
            Expression? currentExpression = null;
            PropertyInfo? lastProperty = null;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                // Ищем свойство
                lastProperty = currentType.GetProperty(
                    part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                if (lastProperty == null)
                {
                    throw new InvalidOperationException(
                        $"Свойство '{part}' не найдено в типе '{currentType.Name}', построение QuerySpecification невозможно.");
                }

                if (parameter == null)
                {
                    parameter = Expression.Parameter(currentType, "x");
                    currentExpression = parameter;
                }

                // x => x.Prop
                currentExpression = Expression.Property(currentExpression!, lastProperty);

                // Переходим к следующему типу
                currentType = GetNavigationElementType(lastProperty.PropertyType);
            }

            if (parameter != null && currentExpression != null && lastProperty != null)
            {
                var lambda = Expression.Lambda(currentExpression!, parameter);
                var chain = new IncludeChain();
                chain.Steps.Add(new IncludeStep(
                    lambda,
                    currentType,
                    lastProperty.PropertyType));
                IncludeChains.Add(chain);

                return new IncludeBuilder<TEntity, TProperty>(this, chain);
            }
            else
            {
                throw new InvalidOperationException($"Не удалось разобрать {nameof(navigationPropertyPath)}");
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IncludeBuilder<TEntity, object> Include(string navigationPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(navigationPropertyPath))
                throw new ArgumentException("Передан пустой путь, построение QuerySpecification невозможно", nameof(navigationPropertyPath));

            var parts = navigationPropertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw new ArgumentException("Передан некорректный путь, построение QuerySpecification невозможно", nameof(navigationPropertyPath));

            Type currentType = typeof(TEntity);
            ParameterExpression parameter = Expression.Parameter(currentType, "x");
            Expression currentExpression = parameter;
            PropertyInfo? lastProperty = null;

            foreach (var part in parts)
            {
                // Ищем свойство
                lastProperty = currentType.GetProperty(
                    part,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase);

                if (lastProperty == null)
                {
                    throw new InvalidOperationException(
                        $"Свойство '{part}' не найдено в типе '{currentType.Name}', построение QuerySpecification невозможно.");
                }

                // x = x.Prop
                currentExpression = Expression.Property(currentExpression, lastProperty);

                // Переход к следующему типу
                currentType = GetNavigationElementType(lastProperty.PropertyType);
            }

            // x => x.Prop
            var lambda = Expression.Lambda(currentExpression, parameter);
            var chain = new IncludeChain();
            chain.Steps.Add(new IncludeStep(
                lambda,
                currentType,
                lastProperty!.PropertyType));

            IncludeChains.Add(chain);

            return new IncludeBuilder<TEntity, object>(this, chain);
        }

        
        // Include для коллекций: возвращаем IncludeBuilder с TProperty == элемент коллекции
        public IncludeBuilder<TEntity, TElement> Include<TElement>(Expression<Func<TEntity, List<TElement>>> navigationPropertyPath)
        {
            var chain = new IncludeChain();
            chain.Steps.Add(new IncludeStep(navigationPropertyPath, typeof(TEntity), navigationPropertyPath.ReturnType));
            IncludeChains.Add(chain);
            return new IncludeBuilder<TEntity, TElement>(this, chain);
        }

        public IQuerySpecification<TEntity> OrderByAsc(Expression<Func<TEntity, object>> keySelector)
        {
            Orderings.Add(new SortOrder<TEntity> { KeySelector = keySelector, Descending = false });
            return this;
        }

        public IQuerySpecification<TEntity> OrderByDesc(Expression<Func<TEntity, object>> keySelector)
        {
            Orderings.Add(new SortOrder<TEntity> { KeySelector = keySelector, Descending = true });
            return this;
        }

        public IQuerySpecification<TEntity> ThenByAsc(Expression<Func<TEntity, object>> keySelector)
            => OrderByAsc(keySelector);

        public IQuerySpecification<TEntity> ThenByDesc(Expression<Func<TEntity, object>> keySelector)
            => OrderByDesc(keySelector);
        
        public static Type GetNavigationElementType(Type type)
        {
            // List<T> → T
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }

            // Reference navigation
            return type;
        }
        
    }

