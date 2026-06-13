using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure;

public static class ExcelReader
{
    public static Result<List<Dictionary<string, object?>>> LoadTable(Stream fileStream, 
                                                                      string sheetName, 
                                                                      int colNamesRowNum, 
                                                                      int firstDataRowNum,
                                                                      ILogger? logger = null)
    {
        try
        {
            if (firstDataRowNum <= colNamesRowNum)
            {
                string err = $"Номер строки с данными должен быть больше чем номер строки с заголовками колонок";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }
            var listData = new List<Dictionary<string, object?>>();

            using SpreadsheetDocument doc = SpreadsheetDocument.Open(fileStream, false);
            
            WorkbookPart? workbookPart = doc.WorkbookPart;
            if (workbookPart == null)
            {
                string err = "Ошибка получения WorkbookPart";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }
                
            Sheet? sheet = workbookPart.Workbook.Descendants<Sheet>()
                .FirstOrDefault(s => s.Name == sheetName);
            if (sheet == null)
            {
                string err = $"Лист с именем {sheetName} не найден!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }

            if (sheet.Id == null || sheet.Id.Value == null)
            {
                string err = $"У листа с именем {sheetName} отсутствует Id!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }
                
            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);

            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null)
            {
                string err = $"Лист с именем {sheetName}: ошибка получения SheetData!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }

            var rows = sheetData.Elements<Row>().ToList();
            if (colNamesRowNum > rows.Count)
            {
                string err = $"Лист с именем {sheetName}: заданный номер строки с именами ({colNamesRowNum}) больше общего числа строк ({rows.Count})!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }

            if (firstDataRowNum > rows.Count)
            {
                string err = $"Лист с именем {sheetName}: номер первой строки с данными ({firstDataRowNum}) больше общего числа строк ({rows.Count})!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }

            // Получаем заголовки столбцов
            Row headerRow = rows[colNamesRowNum - 1];
            SharedStringTable? sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (sharedStrings == null)
            {
                string err = $"Лист с именем {sheetName}: ошибка получения SharedStringTable!";
                logger?.LogError(err);
                return Result<List<Dictionary<string, object?>>>.Error(err);
            }
                
            List<string> headers = headerRow.Elements<Cell>().Select(c => GetCellValue(c, sharedStrings)).ToList();

            // Читаем остальные строки
            for (int i = firstDataRowNum-1; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowData = new Dictionary<string, object?>();
                var cells = row.Elements<Cell>().ToList();

                for (int j = 0; j < headers.Count; j++)
                {
                    string columnName = headers[j];
                    object? value = j < cells.Count ? GetTypedCellValue(cells[j], sharedStrings) : null;
                    rowData[columnName] = value;
                }

                listData.Add(rowData);
            }

            return Result<List<Dictionary<string, object?>>>.Success(listData);
        }
        catch (Exception ex)
        {
            string err = $"Ошибка при чтении файла: {ex.Message}";
            logger?.LogError(ex, err);
            return Result<List<Dictionary<string, object?>>>.Error(err, ex);
        }
    }

    private static object? GetTypedCellValue(Cell cell, SharedStringTable sharedStrings)
    {
        object value = GetCellValue(cell, sharedStrings);
        if (value.ToString() == "") return value;

        if (cell.DataType == null)
        {
            return value;
        }
        
        if (cell.DataType.Value == CellValues.Boolean)
        {
            return value.ToString() == "1";
        }
        else if (cell.DataType.Value == CellValues.Number)
        {
            if (double.TryParse(value.ToString(), out double num))
                return num;
            return value;
        }
        else if (cell.DataType.Value == CellValues.Date)
        {
            if (double.TryParse(value.ToString(), out double dateVal))
                return DateTime.FromOADate(dateVal);
            return value;
        }
        else
        {
            return value;
        }
    }

    private static string GetCellValue(Cell cell, SharedStringTable sharedStrings)
    {
        if (cell.CellValue == null) return "";

        string value = cell.CellValue.Text;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            return sharedStrings.ElementAt(int.Parse(value)).InnerText;
        }

        return value;
    }
}