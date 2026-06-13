using Frame.Rep.DataSourceServices;
using Frame.Rep.Interfaces;
using Frame.Rep.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Frame.Rep
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Здесь будем добавлять все необходимые зависимости для отчетов на уровне ядра
        /// </summary>
        /// <param name="services"></param>
        /// <param name="reportSettings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IServiceCollection AddFrameRepServices(this IServiceCollection services, 
                                                                FrameReportSettings reportSettings,
                                                                ILogger? logger)
        {
            logger?.LogInformation("Конфигурирование FrameRepServices...");

            // Добавляем сервис работы с отчётами
            services.AddTransient(typeof(IFrameReporting), typeof(FrameReporting));

            _forceLoadAssemblies();
            
            // Сервис для формирования метаданных для использования в отчетах и иных сервисах
            services.AddTransient<EntityMetadataDataSourceService>();
            
            // Сервис для создания отчётов формате Docx
            services.AddTransient<ReportBuilderDocx>();

            // Сервис для создания отчётов в формате Excel
            services.AddTransient<ReportBuilderXls>();

            // Сервис для создания отчётов формате Powerpoint
            services.AddTransient<ReportBuilderPptx>();
            
            logger?.LogInformation("Конфигурирование FrameRepServices завершено");

            return services;
        }

        // Обеспечиваем принудительную загрузку сборок, содержащих необходимые библиотечные типы
        // Это нужно, чтобы эти типы сразу же были доступны в скриптах
        private static void _forceLoadAssemblies()
        {
            var x1 = typeof(ClosedXML.Excel.IXLWorkbook);
            var x2 = typeof(ClosedXML.Report.Parameter);
            var x3 = typeof(DocxTemplater.DocxTemplate);
            var x4 = typeof(DocxTemplater.Images.ImageFormatter);
            var x5 = typeof(DocxTemplater.Markdown.MarkdownFormatter);
        }
    }
}
