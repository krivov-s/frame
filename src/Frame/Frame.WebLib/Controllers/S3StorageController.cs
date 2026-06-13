using Frame.App.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Frame.App.FileRepositories;
using Frame.Shared;
using Frame.Infrastructure.FileRepositories;

namespace Frame.WebLib.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class S3StorageController : Controller
    {
        protected ILogger<S3StorageController>? _logger { get; set; }
        protected IGetCurrentUserNameService? _currentUserService { get; set; }
        protected IFilesRepository? _fileRepository { get; set; }
        protected S3Settings? _s3options { get; set; }

        public S3StorageController( IGetCurrentUserNameService? currentUserService,
                                    ILogger<S3StorageController>? logger,
                                    IFilesRepository? fileRepository,
                                    S3Settings? s3Options)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _fileRepository = fileRepository;
            _s3options = s3Options;
        }

        [HttpGet("{strKey}")]
        public async Task<IActionResult> GetFile(string strKey)
        {
            if(_fileRepository != null)
            {
                try
                {
                    Result<byte[]> resFile = await _fileRepository.GetFileAsync(strKey);
                    if(!resFile.IsError && resFile.Value != null)
                    {
                        return File(resFile.Value, "application/octet-stream", strKey);
                    }
                    else
                    {
                        return BadRequest(resFile.ErrorResult);
                    }
                }
                catch (Exception ex)
                {
                    string strMsg = $"Ошибка чтения файла с ключом {strKey} из хранилища: {ex.Message}";
                    _logger?.LogError(strMsg);
                    return BadRequest(strMsg);
                }
            }
            else
            {
                string strMsg = $"Отсутствует доступ к репозиторию при попытке чтения файла с ключом {strKey}!";
                _logger?.LogError(strMsg + " (_fileRepository == null)");
                return BadRequest("Отсутствует доступ к репозиторию!");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Не передан файл для загрузки в хранилище");

            string strKey = file.Name;

            if (_fileRepository == null)
            {
                string strMsg = $"Отсутствует доступ к репозиторию при попытке сохранения файла с ключом {strKey}!";
                _logger?.LogError(strMsg + " (_fileRepository == null)");
                return BadRequest("Отсутствует доступ к репозиторию!");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                await _fileRepository.PutFileAsync(file.FileName, memoryStream);
                return Ok($"Файл с ключом {strKey} успешно загружен в репозиторий");
            }
            catch (Exception ex)
            {
                string strMsg = $"Ошибка загрузки в хранилище файла с ключом {strKey} из хранилища: {ex.Message}";
                _logger?.LogError(strMsg);
                return BadRequest(strMsg);
            }
        }
    }
}
