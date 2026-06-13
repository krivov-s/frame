using Frame.App.FileRepositories;
using Frame.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Frame.WebLib.Controllers;

// [ApiController]
[Route("api/getfile")]
public class GetFileController(ILogger<GetFileController> logger, IFilesRepository fileRepository)
    : Controller
{
    private ILogger<GetFileController>? _logger { get; set; } = logger;
    private IFilesRepository? _fileRepository { get; set; } = fileRepository;

    [HttpGet]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileKey, [FromQuery] string fileName)
    {
        if (_fileRepository == null)
        {
            string err = "GetFileController: не был получен экземпляр FileRepository!";
            _logger?.LogError(err);
            return StatusCode(500, err);
        }

        if (string.IsNullOrEmpty(fileKey))
        {
            string err = "GetFileController: Ключ файла для загрузки из хранилища не указан!";
            _logger?.LogError(err);
            return BadRequest(err);
        }

        if (string.IsNullOrEmpty(fileName))
        {
            string err = "GetFileController: Имя файла для сохранения на стороне клиента не указано!";
            _logger?.LogError(err);
            return BadRequest(err);
        }

        try
        {
            Result<byte[]> resBytes = await _fileRepository.GetFileAsync(fileKey);
            if (resBytes.IsError)
            {
                string err = $"Ошибка скачивания файла {fileKey} из хранилища: {resBytes.ErrorResult}";
                return StatusCode(500, err);
            }
            else if (resBytes.Value == null)
            {
                string err = $"При загрузке файла {fileKey} из хранилища вернулся пустой файл";
                return StatusCode(500, err);
            }
            else
            {
                byte[] bytes = resBytes.Value;
                MemoryStream stream = new MemoryStream(bytes);
                return File(stream, "application/octet-stream", fileName);
            }
        }
        catch (Exception ex)
        {
            string err = $"Ошибка скачивания файла {fileKey}: {ex.Message}";
            _logger?.LogError(ex, err);
            return StatusCode(500, err);
        }
    }
}