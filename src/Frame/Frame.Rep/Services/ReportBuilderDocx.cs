using System.Dynamic;
using Frame.App.Cores;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Params;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using ClosedXML.Report;
using DocxTemplater;
using DocxTemplater.Images;
using DocxTemplater.Markdown;
using Frame.Domain.Entities.Core;

namespace Frame.Rep.Services
{
    public class ReportBuilderDocx(ILogger<IReportBuilder> logger, IFilesRepository filesRepository,
        AppCoreProvider appCoreProvider, IObjectStorageProvider objectStorageProvider,
        IScriptCore scriptCore) : IReportBuilder
    {
        private readonly ILogger<IReportBuilder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IFilesRepository _filesRepository = filesRepository ?? throw new ArgumentNullException(nameof(filesRepository));
        private readonly AppCoreProvider _appCoreProvider = appCoreProvider ?? throw new ArgumentNullException(nameof(appCoreProvider));
        private readonly IObjectStorageProvider _objectStorageProvider = objectStorageProvider ?? throw new ArgumentNullException(nameof(objectStorageProvider));
        private readonly IScriptCore _scriptCore = scriptCore ?? throw new ArgumentNullException(nameof(scriptCore));

        public async Task<Result<MemoryStream>> GenerateReportAsync(FrameReport report, ParamList paramList, List<dynamic>? entities = null, IObjectStorage? storage = null)
        {
            try
            {
                //Префикс для более понятного логгирования.
                string logPrefix = (report.Name.Length > 0)
                        ? $"Отчет {report.Name}"
                        : $"Отчет с Id = {report.Id}";

                // ----------------------------------------------------------------------
                // 1. Получаем данные для отчёта.
                // ----------------------------------------------------------------------
                await ReportHelpers.DebugLogAsync("Начало формирования", report, _logger, _objectStorageProvider, true, true);
                Result<List<dynamic>> resList = await ReportHelpers.LoadData(logPrefix, report, 
                    paramList, _logger, _scriptCore, _objectStorageProvider, storage, entities);
                if (resList.IsErrorOrNull)
                {
                    return Result<MemoryStream>.Error(resList);
                }
                await ReportHelpers.DebugLogAsync($"Данные прочитаны", report, _logger, objectStorageProvider, true);

                List<dynamic> listRecords = resList.Value!;
                _logger.LogInformation("{LogPrefix}: получено {Count} записей.", logPrefix, listRecords.Count);

                // ----------------------------------------------------------------------
                // 2. Читаем шаблон отчета
                // ----------------------------------------------------------------------
                Result<MemoryStream> resTemplate = await ReportHelpers.ReadReportTemplateToMemoryAsync(report, 
                    _filesRepository, logPrefix, _logger);
                if (resTemplate.IsErrorOrNull)
                {
                    return resTemplate;
                }
                await ReportHelpers.DebugLogAsync($"Шаблон считан", report, _logger, objectStorageProvider, false);

                MemoryStream memoryStreamSource = resTemplate.Value!;

                DocxTemplate template = new DocxTemplate(memoryStreamSource);
                template.RegisterFormatter(new ImageFormatter());
                //template.RegisterFormatter(new MarkDownFormatter());

                // ----------------------------------------------------------------------
                // 3. Формируем объект - источник данных для отчета
                // ----------------------------------------------------------------------
                Result<AppCore> appCore = await _appCoreProvider.GetAppCoreAsync();
                if (appCore.IsError)
                {
                    return ReportHelpers.LogAndReturnError(logPrefix, "ошибка получения AppCore", _logger);
                }

                dynamic dataSource = new ExpandoObject();
                
                dataSource.Entities = entities;
                dataSource.AppCore = appCore;
                dataSource.ParamList = paramList;
                if (listRecords.Count > 0 && listRecords[0] is BaseEntity entity)
                {
                    dataSource.Obj = entity;
                }

                // ----------------------------------------------------------------------
                // 4. Запускаем формирование отчета
                // ----------------------------------------------------------------------
                await ReportHelpers.DebugLogAsync($"Подготовка выполнена, запускаем генератор отчета", report, 
                    _logger, objectStorageProvider, true);
                MemoryStream streamResult = new MemoryStream();
                template.BindModel("ds", dataSource);
                template.Save(streamResult);
                
                await ReportHelpers.DebugLogAsync($"Подготовка отчета завершена", report, _logger, 
                    objectStorageProvider, true);
                return Result<MemoryStream>.Success(streamResult);
            }
            catch (Exception exc)
            {
                string err = $"Ошибка при формировании отчёта {report}: {exc.Message}";
                _logger.LogError(exc, err);
                return Result<MemoryStream>.Error(err);
            }
        }
    }
}
