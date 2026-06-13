using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Frame.Rep.Services;

/// <summary>
/// Кэширующий вычислитель выражений
/// </summary>
public static class ExpressionEvaluator
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<Script<object>>>> _cache = new();
    private static readonly ScriptOptions _scriptOptions = ScriptOptions.Default
        .WithReferences(typeof(object).Assembly)
        .AddReferences("System", "System.Core", "Microsoft.CSharp")
        .AddImports("System");

    private static readonly int _maxCacheSize = 1000; // Ограничение по количеству кэшируемых выражений

    public static async Task<object?> EvaluateAsync(string expression, object globals)
    {
        if (expression == null || expression.Length == 0)
            return null;
        // Если кэш переполнен, очищаем его
        if (_cache.Count > _maxCacheSize)
        {
            _cache.Clear();
        }

        // Получаем или компилируем скрипт (Lazy<Task> предотвращает дублирующуюся компиляцию)
        var scriptLazy = _cache.GetOrAdd(expression, expr =>
            new Lazy<Task<Script<object>>>(() => Task.Run(() => 
                CSharpScript.Create(expr, _scriptOptions, globals.GetType())))
        );

        var script = await scriptLazy.Value; // Дожидаемся компиляции скрипта
        object res = await script.RunAsync(globals).ContinueWith(t => t.Result.ReturnValue);
        return res;
    }
}
