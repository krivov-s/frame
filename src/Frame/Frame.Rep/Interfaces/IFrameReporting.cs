using Frame.App.IEntityRepositories;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Core.Scripting;
using Frame.Domain.Params;
using Frame.Shared;
using Frame.Domain.Entities.Core.Queries;

namespace Frame.Rep.Interfaces
{
    /// <summary>
    /// Сервис подготовки отчетов
    /// </summary>
    public interface IFrameReporting
    {
        /// <summary>
        /// Подготовка отчета, определяемого аргументом <see cref="report"/>. Список параметров <see cref="paramList"/>
        /// содержит результирующий список параметров, объединенный с параметрами источника данных.
        /// Источником данных отчета может являться <see cref="SavedQuery"/>, <see cref="ScriptCommand"/> или
        /// уже готовый набор данных <see cref="entities"/> (обычно это список объектов, выбранных пользователем в списке,
        /// из которого запускается отчет)
        /// <para>
        /// Порядок определения источника данных:
        /// <list type="number">
        /// <item>аргумент query</item>
        /// <item>SavedQuery отчета</item>
        /// <item>ScriptCommand отчета</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="report">Объект, описывающий отчет</param>
        /// <param name="paramList">Заполненный список параметров отчета</param>
        /// <param name="entities">Список объектов, на основе которых строится отчет.</param>
        /// <param name="storage">Хранилище, из которого были считаны объекты <see cref="entities"/></param>
        /// <returns></returns>
        public Task<Result<MemoryStream>> GenerateReportAsync(FrameReport report, ParamList paramList, 
            List<dynamic>? entities = null, IObjectStorage? storage = null);
    }
}
