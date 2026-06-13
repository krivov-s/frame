using Frame.App.Cores;
using Frame.App.FileRepositories;
using Frame.App.IEntityRepositories;
using Frame.App.Scripting;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Params;
using Frame.Rep.Interfaces;
using Frame.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.Rep.Services
{
    public class FrameReporting(ILogger<IFrameReporting> logger, IServiceProvider serviceProvider) : IFrameReporting
    {
        private readonly ILogger<IFrameReporting> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public async Task<Result<MemoryStream>> GenerateReportAsync(FrameReport report, ParamList paramList, 
            List<dynamic>? entities = null, IObjectStorage? storage = null)
        {
            try
            {
                string? extension = report.ReportTemplate?.FileName.Split('.').Last();
                string extensionError = $"Неверный шаблон отчёта {report}";
                
                if (string.IsNullOrEmpty(extension))
                {
                    _logger.LogError(extensionError);
                    return Result<MemoryStream>.Error($"Неверный шаблон отчёта {report}");
                }

                IReportBuilder? reportBuilder = null; 
                switch (extension.ToLower())
                {
                    case nameof(FileExtension.pptx):
                    {
                        reportBuilder = _serviceProvider.GetService<ReportBuilderPptx>();
                        break;
                    }
                    case nameof(FileExtension.xlsx):
                    {
                        reportBuilder = _serviceProvider.GetService<ReportBuilderXls>();
                        break;
                    }
                    case nameof(FileExtension.docx):
                    {
                        reportBuilder = _serviceProvider.GetService<ReportBuilderDocx>();
                        break;
                    }
                    default:
                    {
                        _logger.LogError("{Err}", extensionError);
                        return Result<MemoryStream>.Error($"Неверный шаблон отчёта {report}");
                    }
                }

                if (reportBuilder == null)
                {
                    string err = "Не удалось получить ReportBuilder для отчета '{extension}'";
                    _logger.LogError("{Err}", err);
                    return Result<MemoryStream>.Error(err);
                }
                return await reportBuilder.GenerateReportAsync(report, paramList, entities, storage);
            }
            catch (Exception exc)
            {
                string err = $"Ошибка при формировании отчёта {report}: {exc.Message}";
                _logger.LogError(exc, "{Err}", err);
                return Result<MemoryStream>.Error(err);
            }
        }
    }
}
