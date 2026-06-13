using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Frame.Domain.Entities.Metadata;
using Microsoft.AspNetCore.Http;

namespace Frame.WebLib.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportEntityController : ControllerBase
{
    /// <summary>
    /// Экспорт одной сущности по типу и идентификатору
    /// Пример: <![CDATA[
    /// GET /api/ExportEntity/ExportEntity?entityType=User&entityId=123
    /// ]]> 
    /// </summary>
    [HttpGet("ExportEntity")]
    public IActionResult ExportEntity([FromQuery] string entityType, [FromQuery] int entityId)
    {
        Type? type = EntityMetadata.GetEntityType(entityType);
        if (type == null) return BadRequest("Передан некорректный тип: не удалось определить объект");

        return Ok("Метод еще не реализован.");

        // TODO: доделать чтение объекта из БД и его экспорт.
        // Код ниже - в целом рабочий, нужно просто аккуратно все допилить.

        // var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        // var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        //
        // var fileName = $"{entityType}_{entityId}.json";
        // return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Экспорт списка сущностей
    /// Пример: POST /api/ExportEntity/ExportEntityList
    /// Тело запроса: { "entityType": "User", "entityId": [1,2,3] }
    /// </summary>
    [HttpPost("ExportEntityList")]
    public IActionResult ExportEntityList([FromBody] ExportListRequest request)
    {
        // Заглушка — вместо этого вытягиваем список из БД
        var objList = request.EntityId.Select(id => new
        {
            EntityType = request.EntityType,
            Id = id,
            Name = $"Sample {request.EntityType} {id}",
            Created = DateTime.UtcNow
        }).ToList();

        var json = JsonSerializer.Serialize(objList, new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var fileName = $"{request.EntityType}_list.json";
        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Импорт списка сущностей из JSON-файла
    /// Пример: POST /api/ExportEntity/ImportEntityList (multipart/form-data)
    /// </summary>
    [HttpPost("ImportEntityList")]
    public async Task<IActionResult> ImportEntityList([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не был загружен.");

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        try
        {
            var entities = JsonSerializer.Deserialize<List<GenericEntity>>(json);
            if (entities == null)
                return BadRequest("Не удалось разобрать JSON.");

            // TODO: сохранить в БД или выполнить обработку
            return Ok(new
            {
                ImportedCount = entities.Count,
                Items = entities
            });
        }
        catch (JsonException ex)
        {
            return BadRequest($"Ошибка при разборе JSON: {ex.Message}");
        }
    }
}

/// <summary>
/// DTO для экспорта списка сущностей
/// </summary>
public class ExportListRequest
{
    public string EntityType { get; set; } = string.Empty;
    public List<int> EntityId { get; set; } = new();
}

/// <summary>
/// Универсальный DTO для демонстрации импорта
/// </summary>
public class GenericEntity
{
    public string EntityType { get; set; } = string.Empty;
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime Created { get; set; }
}
