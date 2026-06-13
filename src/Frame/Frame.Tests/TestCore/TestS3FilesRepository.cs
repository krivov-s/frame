using FluentAssertions;
using Frame.Infrastructure.FileRepositories;
using Frame.Shared;

namespace Frame.Tests.TestCore
{
    [Collection("DisableParallelism")]
    public class TestS3FilesRepository
    {
        private S3Settings settings = new()
        {
            ServiceUrl = "https://storage.yandexcloud.net",
            AccessKey = "KEY",
            SecretKey = "SECRET",
            BucketName = "photos"
        };

        const string PictureFileName = "..\\..\\..\\files\\nature.jpg";

        //[Fact]
        public async Task Put_File_Ret_True()
        {
            S3FilesRepository repo = new S3FilesRepository(settings);
            using FileStream file = File.OpenRead(PictureFileName);
            Result result = await repo.PutFileAsync("test_pic.jpg", file);
            result.IsError.Should().BeFalse();
        }

        //[Fact]
        public async Task Get_File_Ret_True()
        {
            S3FilesRepository repo = new S3FilesRepository(settings);
            Result<byte[]> result = await repo.GetFileAsync("test_pic.jpg");
            result.IsError.Should().BeFalse();
            result.Value.Should().NotBeNull();
            result?.Value?.Length.Should().BeGreaterThan(0);
        }
    }
}
