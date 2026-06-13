using ClosedXML.Excel;

namespace Frame.Rep.Services
{
    /// <summary>
    /// Класс содержит в себе данные, необходимые для заполнения отчётов.
    /// </summary>
    public class ScriptCellData
    {
        /// <summary>
        /// Номер колонки ячейки.
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Скрипт вида {Obj.Address}, содержащийся в ячейке.
        /// </summary>
        public string? Script { get; set; }

        /// <summary>
        /// Стиль ячейки.
        /// </summary>
        public IXLStyle? Style { get; set; }
    }
}
