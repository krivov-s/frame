using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Frame.App.FileRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;

namespace Frame.Infrastructure.FileRepositories
{
    public class Test { }

    /// <summary>
    /// Хранилище файлов в хранилище S3. Конфигурация считывается из appsettings.json
    /// </summary>
    public class S3FilesRepository : IFilesRepository
    {
        private readonly S3Settings _options;
        private readonly ILogger<S3FilesRepository>? _logger;
        private AmazonS3Client? _client;

        public S3FilesRepository(IFileRepositirySettings settings, ILogger<S3FilesRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(settings);

            S3Settings? s3Settings = settings as S3Settings;

            if (s3Settings == null)
            {
                _logger?.LogError("{Repository}: переданный объект с настройками не является объектом с типом {tSettings}"
                    ,nameof(S3FilesRepository), nameof(S3Settings));
                throw new ArgumentNullException(nameof(settings));
            }
            
            _options = s3Settings;
            _logger = logger;
        }

        // public S3FilesRepository(S3Settings settings, ILogger<S3FilesRepository>? logger = null) 
        // {
        //     _options = settings;
        //     _logger = logger;
        // }

        public async Task<Result> PutFileAsync(string strKey, Stream file)
        {
            Result r1 = CheckInitialized();
            if(r1.IsError || _client == null) 
            { 
                return r1; 
            }
            //FileStream file = System.IO.File.OpenRead(fileName);
            var request = new PutObjectRequest()
            {
                BucketName = _options.BucketName,
                Key = strKey,
                InputStream = file
            };
            ////request.Metadata.Add("Content-Type", file.ContentType);
            try
            {
                await _client.PutObjectAsync(request);
                return Result.Success;
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка сохранения файла {strKey} в хранилище: {ex.Message}";
                _logger?.LogError("{strError}, {ex}", strError, ex.StackTrace);
                return Result.Error(strError);
            }
        }
        public async Task<Result<byte[]>> GetFileAsync(string strKey)
        {
            Result r1 = CheckInitialized();
            if (r1.IsError || _client == null)
            {
                return Result<byte[]>.Error(r1.ErrorResult);
            }
            try
            {
                GetObjectResponse response = await _client.GetObjectAsync(_options.BucketName, strKey);
                using Stream stream = response.ResponseStream;
                using MemoryStream memoryStream = new();
                await stream.CopyToAsync(memoryStream);
                return Result<byte[]>.Success(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                string strError = $"Ошибка получения файла {strKey} из хранилища: {ex.Message}";
                _logger?.LogError(ex, "{Err}", strError);
                return Result<byte[]>.Error(strError);
            }
            //FileStream file = System.IO.File.CreateTLS(fileName);
            //response.ResponseStream.CopyTo(file);
            //file.Close();
            //throw new NotImplementedException();
        }

        public Task<Result> DeleteFileAsync(string strKey)
        {
            return Task.FromResult(Result.Error("Данная опция для хранилища S3 еще не реализована!"));
        }

        protected virtual Result CheckInitialized()
        {
            if(_client != null)
            {
                return Result.Success;
            }
            if(_options == null )
            {
                string strError = "В объект S3Repository не передан S3Settings. Подключение невозможно.";
                _logger?.LogError(strError);
                return Result.Error(strError);
            }
            if(_options.SecretKey.Length == 0)
            {
                string strError = "В параметрах не передан SecretKey. Подключение невозможно.";
                _logger?.LogError(strError);
                return Result.Error(strError);
            }
            if (_options.AccessKey.Length == 0)
            {
                string strError = "В параметрах не передан AccessKey. Подключение невозможно.";
                _logger?.LogError(strError);
                return Result.Error(strError);
            }

            var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl
            };

            _client = new AmazonS3Client(credentials, config);
            return Result.Success;
        }

        public Task<Result<List<string>>> LoadFileListAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}
