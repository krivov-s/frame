using System.Dynamic;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Params;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Frame.App.FileRepositories;
using Frame.Domain.Entities.Core.Queries;

namespace Frame.Rep
{
    public static class ReportHelpers
    {
        public static async Task<Result<List<dynamic>>> LoadData(string logPrefix, FrameReport report, ParamList paramList,
                    ILogger<IReportBuilder> logger, IScriptCore scriptCore, 
                    IObjectStorageProvider objectStorageProvider, IObjectStorage? storage = null, List<dynamic>? entities = null)
        {
            Result<List<dynamic>> res;
            if (report.ScriptCommand != null)
            {
                // Приоритет 1. Автооперация. Она может каким-то образом обработать, изменить, скорректировать список.
                res = await LoadDataFromScriptCommandAsync(logPrefix, report.ScriptCommand, paramList, entities, logger, scriptCore, storage);
            }
            else if (entities != null)
            {
                // Приоритет 2. Если передан готовый список и не передана автооперация - просто используем его.
                res = Result<List<dynamic>>.Success(entities);
            }
            else if (report.SavedQuery != null)
            {
                // Приоритет 3. Сохраненный запрос.
                res = await LoadDataFromSavedQueryAsync(logPrefix, report.SavedQuery, paramList, objectStorageProvider, storage);
            }
            else
            {
                // Вовращаем пустой список
                logger.LogInformation("Источник данных для {Descr} отсутствует: используется пустой список.", report.Description);
                return Result<List<dynamic>>.Success([]);
            }

            return res;
        }

        public static async Task<Result<List<ExpandoObject>>> LoadDataAsExpando(string logPrefix, FrameReport report,
            ParamList paramList, ILogger<IReportBuilder> logger, IScriptCore scriptCore,
            IObjectStorageProvider objectStorageProvider, IObjectStorage? storage = null,
            List<dynamic>? entities = null)
        {
            Result<List<dynamic>> res = await LoadData(logPrefix, report, paramList, logger, scriptCore,
                objectStorageProvider, storage, entities);
            
            if (res.IsErrorOrNull) return Result<List<ExpandoObject>>.Error(res.ErrorResult);

            List<ExpandoObject> list = new();
            
            foreach (var value in res.Value!)
            {
                if (value is BaseEntity entity)
                {
                    list.Add(entity.ToExpando());
                }
                else
                {
                    string err = $"{logPrefix}: в список передан объект, не являющийся {nameof(BaseEntity)}.";
                    logger.LogError("{Err}", err);
                    return Result<List<ExpandoObject>>.Error(err);
                }
            }
            
            return Result<List<ExpandoObject>>.Success(list);
        }

        public static async Task<Result<List<dynamic>>> LoadDataFromSavedQueryAsync(string logPrefix,
                    SavedQuery savedQuery, ParamList paramList, IObjectStorageProvider objectStorageProvider, IObjectStorage? storage = null)
        {
            if (storage == null)
            {
                Result<IObjectStorage> res = objectStorageProvider.GetObjectStorage();
                if (res.IsError || res.Value == null)
                {
                    string err = $"{logPrefix}: ошибка получения {nameof(IObjectStorage)}: {res.ErrorResult}";
                    return Result<List<dynamic>>.Error(err);
                }
                storage = res.Value;
            }

            Result<List<dynamic>> resList = await storage.GetDynamicListAsync(savedQuery, paramList, logPrefix);
            if (resList.IsError || resList.Value == null)
            {
                return Result<List<dynamic>>.Error(resList);
            }

            return Result<List<dynamic>>.Success(resList.Value);
        }

        public static async Task<Result<List<dynamic>>> LoadDataFromScriptCommandAsync(string logPrefix,
                    ScriptCommand scriptCommand, ParamList paramList, List<dynamic>? entities, 
                    ILogger<IReportBuilder> logger, IScriptCore scriptCore, IObjectStorage? storage = null)
        {
            if (scriptCommand.CommandType != ECommandType.DataSource)
            {
                string err = $"{logPrefix}: источник данных ScriptCommand ({scriptCommand}) " +
                             $"должен иметь тип {nameof(ECommandType.DataSource)}";
                logger.LogError(err);
                return Result<List<dynamic>>.Error(err);
            }
            Result<dynamic> resCmd = await scriptCore.ExecuteScriptCommandAsync(scriptCommand.Id, paramList, entities, storage);
            if (resCmd.IsError || resCmd.Value == null)
            {
                return Result<List<dynamic>>.Error(resCmd);
            }

            List<dynamic>? list = resCmd.Value;
            if (list == null)
            {
                string err = $"{logPrefix}: результат выполнения ScriptCommand не является списком List<dynamic>";
                logger.LogError(err);
                return Result<List<dynamic>>.Error(err);
            }

            return Result<List<dynamic>>.Success(list);
        }

        public static Result<MemoryStream> LogAndReturnError(string errorPrefix, string errorMsg, ILogger<IReportBuilder> logger)
        {
            string err = $"{errorPrefix}: {errorMsg}";
            logger.LogError("{Err}", err);
            return Result<MemoryStream>.Error(err);
        }

        public static async Task<object?> ExecuteStringWithScript(string source, ScriptExecutionGlobals globals, FrameReport report,
                                                                    string imageTag, ILogger<IReportBuilder> logger, IObjectStorageProvider objectStorageProvider, 
                                                                    IScriptCore scriptCore, IObjectStorage? loggerStorage = null)
        {
            MatchCollection matches = Regex.Matches(source, "{([^}]*)}");

            if (matches.Count == 0)
            {
                return source;
            }

            // Если в source только значение для вычисления, то будем возвращать его без конвертации в string
            bool bValueOnly = matches.Count == 1 && source.First() == '{' && source.Last() == '}';

            foreach (Match match in matches)
            {
                string stringWithScript = match.Value;
                string script = stringWithScript.Replace("}", "").Replace("{", "");

                await DebugLogAsync($"Запуск на исполнение скрипта {script}", report, logger, objectStorageProvider, false);

                Result<dynamic> res = scriptCore.ExecuteScriptFunction(script, globals);

                //Если результат выполенния скрипта является null(т.е. нет результата), то мы убираем из строки изначальный скрипт
                if (res.IsErrorOrNull)
                {
                    source = "";
                    continue;
                }

                string? resultString;

                if (res.Value is DateTime dateTimeResult)
                {
                    resultString = dateTimeResult.ToString("dd.MM.yyyy");
                }
                else if (res.Value is DateOnly dateResult)
                {
                    resultString = dateResult.ToString("dd.MM.yyyy");
                }
                else
                {
                    resultString = res.Value!.ToString();
                }

                source = source.Replace(match.Value, resultString);

                //Если здесь не привести к необходимой форме DateTime, то он будет отображаться по-разному относительно 
                //культуры, в которой запущено приложение.
                if(bValueOnly && res.Value is DateOnly)
                {
                    return source;
                }

                if(bValueOnly && res.Value is DateTime)
                {
                    return source;
                }

                if (bValueOnly)
                {
                    return res.Value;
                }

                if (source.Contains(imageTag))
                    source = source.Replace(imageTag, "").Replace(" ", "");
            }

            return source;
        }

        /// <summary>
        /// Метод определяет, есть ли скрипт в строке. Если скрипт есть - выполняет его и возвращает данные.
        /// </summary>
        /// <param name="source">Скрипт.</param>
        /// <param name="globals">Объект, данные которого будут использоваться при выполнении.</param>
        /// <param name="logger">Объект системы логгирования.</param>
        /// <param name="scriptCore">Интерфейс скрипт ядра.</param>
        /// <returns>Возвращает результат выполнения скрипта, либо саму строку, если скрипта в ней не было.</returns>
        /// <exception cref="FrameException"></exception>
        public static dynamic? ExecuteStringFunctionXLS(dynamic source, ScriptExecutionGlobals globals, ILogger<IReportBuilder> logger, IScriptCore scriptCore)
        {
            if (source is not string)
                return source;
            
            string sourceString = source.ToString();

            // Если внури конструкция вида {{expr}} - возвращаем как есть, это будет обработано с помощью ClosedXML.Report
            if (sourceString.StartsWith("{{") && sourceString.EndsWith("}}"))
            {
                return source;
            }
            
            //Находим в строке скрипты, подходящие под паттерн типа {Obj.Address}.
            MatchCollection matches = Regex.Matches(sourceString, "{([^}]*)}");

            if (matches.Count == 0)
            {
                return source;
            }
            
            // Если в source только значение для вычисления, то будем возвращать его без конвертации в string
            bool bValueOnly = matches.Count == 1 && sourceString.First() == '{' && sourceString.Last() == '}';

            foreach (Match match in matches)
            {
                string stringWithScript = match.Value;
                string script = stringWithScript.Replace("}", "").Replace("{", "");

                Result<dynamic> res = scriptCore.ExecuteScriptFunction(script, globals);

                if (res.IsError) res.CheckAndThrow("Выполнение скрипта в строке:");

                if (res.Value != null && bValueOnly)
                {
                    return res.Value;
                }

                string resultString = (res.Value == null) ? "" : res.Value!.ToString();
                sourceString = sourceString.Replace(match.Value, resultString);
            }

            return sourceString;
        }


        /// <summary>
        /// Чтение содержимого файла шаблона отчета в память (MemoryStream)
        /// </summary>
        /// <param name="report">Объект с отчетом</param>
        /// <param name="fileRepo">Репозиторий для чтения содержимого файла шаблона отчета</param>
        /// <param name="logPrefix">Префикс для логгирования ошибок</param>
        /// <param name="logger">Логгер</param>
        /// <returns></returns>
        public static async Task<Result<MemoryStream>> ReadReportTemplateToMemoryAsync(FrameReport report, 
            IFilesRepository fileRepo, string logPrefix, ILogger logger)
        {
            if (report.ReportTemplate == null)
            {
                string err = $"{logPrefix}: в объекте FrameReport не загружен файл-документ с шаблоном отчёта";
                logger.LogError("{Err}", err);
                return Result<MemoryStream>.Error(err);
            }

            if (string.IsNullOrEmpty(report.ReportTemplate.FileKey))
            {
                string err = $"{logPrefix}: в файл-документе отсутствует путь до шаблона отчёта (FileKey)";
                logger.LogError("{Err}", err);
                return Result<MemoryStream>.Error(err);
            }

            Result<byte[]> fileRepositoryResult = await fileRepo.GetFileAsync(report.ReportTemplate.FileKey);

            if (fileRepositoryResult.IsErrorOrNull)
            {
                return Result<MemoryStream>.Error(fileRepositoryResult.ErrorResult);
            }

            MemoryStream memoryStreamSource = new(fileRepositoryResult.Value!);
            return Result<MemoryStream>.Success(memoryStreamSource);
        }
        
        /// <summary>
        /// Использовалось для отладки и поиска причины падения сервера. Пока не удаляем полностью.
        /// </summary>
        private const bool DEBUG_LOG_TO_FRAME_REPORT_DESCR = false;

        public static async Task DebugLogAsync(string msg, FrameReport frameReport, ILogger<IReportBuilder> logger, 
                                                    IObjectStorageProvider objectStorageProvider, bool writeToLog = true, bool bErase = false, IObjectStorage? loggerStorage = null)
        {
            if (writeToLog)
            {
                logger.LogInformation("Отчет {Name}: {Msg}", frameReport.Name, msg);
            }

            if (!DEBUG_LOG_TO_FRAME_REPORT_DESCR)
            {
                return;
            }

            if (loggerStorage == null)
            {
                Result<IObjectStorage> resStg = objectStorageProvider.GetObjectStorage();
                if (resStg is { IsError: false, Value: not null })
                {
                    loggerStorage = resStg.Value;
                }
            }

            if (loggerStorage != null)
            {
                Result<FrameReport> resRep = await loggerStorage.GetObjAsync<FrameReport>(frameReport.Id);
                if (resRep.IsError || resRep.Value == null)
                {
                    return;
                }

                if (bErase)
                {
                    resRep.Value.Descr = "";
                }
                else
                {
                    resRep.Value.Descr = resRep.Value.Descr + "; " + msg;
                }
                await loggerStorage.SaveChangesAsync();
            }
        }
    }
}
