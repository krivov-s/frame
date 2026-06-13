using System.Data;
using Frame.Domain.Entities.Core.Security;
using Microsoft.AspNetCore.Mvc;

namespace Frame.WebLib.Controllers.Schema;

/// <summary>
/// Список пользователей системы
/// </summary>
/// <remarks>
/// Метод планировался для интеграции с FastReport Designer для разработки отчетов без прямого соединения с БД
/// </remarks>
/// <returns></returns>
[ApiController]
[Route("api/schema/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Структура таблицы в формате Xml
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetSchema()
    {
        var dataSet = new DataSet("ReportData");
        var table = CreateDataTable();

        dataSet.Tables.Add(table);

        return ReturnAsFileStream(dataSet);
    }

    /// <summary>
    /// Структура таблицы в формате Xml и минимальный набор тестовых данных
    /// </summary>
    /// <returns></returns>
    [HttpGet("with-test-data")]
    public IActionResult GetSchemaAnTestData()
    {
        var dataSet = new DataSet("ReportData");
        var table = CreateDataTable();

        var sampleData = new List<User>
        {
            new User { Id = 1, Login = "Acme Corp", FIO = "Пупкин" }
        };

        foreach (var user in sampleData)
        {
            table.Rows.Add(user.Id, user.Login, user.FIO);
        }

        dataSet.Tables.Add(table);

        return ReturnAsFileStream(dataSet);
    }

    private IActionResult ReturnAsFileStream(DataSet dataSet)
    {
        var stream = new MemoryStream();
        dataSet.WriteXml(stream, XmlWriteMode.WriteSchema);
        stream.Position = 0;

        return File(stream, "application/xml");
    }

    private static DataTable CreateDataTable()
    {
        var table = new DataTable("Users");

        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Login", typeof(string));
        table.Columns.Add("FIO", typeof(string));
        return table;
    }
}