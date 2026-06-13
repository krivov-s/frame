using System.Text;
using FluentAssertions;
using Frame.Infrastructure.FileRepositories;
using Frame.Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace Frame.Tests.TestCore;

[Collection("DisableParallelism")]
public class TestFileRepository : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileServerRepository _repository;
    private readonly Mock<ILogger<FileServerRepository>> _loggerMock;
    private readonly FileServerSettings _settings;

    public TestFileRepository()
    {
        // Создаем временную директорию для тестов
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileRepositoryTests_{Guid.NewGuid()}");
        _settings = new FileServerSettings() { FileServerPath = _testDirectory };
        _loggerMock = new Mock<ILogger<FileServerRepository>>();
        //var options = Options.Create(_settings);
        _repository = new FileServerRepository(_settings, _loggerMock.Object);
    }

    public void Dispose()
    {
        // Очищаем временную директорию после тестов
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task PutFileAsync_ValidFile_ShouldSaveSuccessfully()
    {
        // Arrange
        var content = "Test content";
        var key = "test.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var result = await _repository.PutFileAsync(key, stream);

        // Assert
        Assert.True(!result.IsError);
        Assert.True(File.Exists(Path.Combine(_testDirectory, key)));
        var savedContent = await File.ReadAllTextAsync(Path.Combine(_testDirectory, key));
        Assert.Equal(content, savedContent);
    }

    [Fact]
    public async Task GetFileAsync_ExistingFile_ShouldReturnContent()
    {
        // Arrange
        var content = "Test content";
        var key = "test.txt";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, key), content);

        // Act
        var result = await _repository.GetFileAsync(key);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        var retrievedContent = Encoding.UTF8.GetString(result.Value!);
        retrievedContent.Should().Be(content);
    }

    [Fact]
    public async Task GetFileAsync_NonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var key = "nonexistent.txt";

        // Act
        var result = await _repository.GetFileAsync(key);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Файл не найден: nonexistent.txt", result.ErrorResult);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PutFileAsync_InvalidKey_ShouldReturnFailure(string? key)
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act
        var result = await _repository.PutFileAsync(key!, stream);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Не указан путь к файлу", result.ErrorResult);
    }

    [Fact]
    public async Task PutFileAsync_NullStream_ShouldReturnFailure()
    {
        // Act
        var result = await _repository.PutFileAsync("test.txt", null!);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Не передан файл для записи", result.ErrorResult);
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleReads_ShouldSucceed()
    {
        // Arrange
        var key = "concurrent_test.txt";
        var content = "Test content";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, key), content);

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _repository.GetFileAsync(key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(!r.IsError));
        Assert.All(results, r => Assert.Equal(content, Encoding.UTF8.GetString(r.Value!)));
    }

    [Fact]
    public async Task ConcurrentAccess_WriteAndRead_ShouldHandleCorrectly()
    {
        // Arrange
        var key = "concurrent_write_read.txt";
        var writeContent = "Test content";
        var readTasks = new List<Task<Result<byte[]>>>();
        var writeTasks = new List<Task<Result>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            writeTasks.Add(Task.Run(async () =>
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(writeContent + i));
                return await _repository.PutFileAsync(key, stream);
            }));
            readTasks.Add(_repository.GetFileAsync(key));
        }

        await Task.WhenAll(writeTasks.Concat<Task>(readTasks));

        // Assert
        var finalResult = await _repository.GetFileAsync(key);
        Assert.True(!finalResult.IsError);
        Assert.True(writeTasks.All(t => !t.Result.IsError));
    }

    [Fact]
    public async Task PutFileAsync_LargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var key = "large_file.txt";
        var content = new byte[5 * 1024 * 1024]; // 5MB
        new Random().NextBytes(content);
        using var stream = new MemoryStream(content);

        // Act
        var putResult = await _repository.PutFileAsync(key, stream);
        var getResult = await _repository.GetFileAsync(key);

        // Assert
        Assert.True(!putResult.IsError);
        Assert.True(!getResult.IsError);
        Assert.True(getResult.Value != null);
        Assert.Equal(content.Length, getResult.Value.Length);
        Assert.Equal(content, getResult.Value);
    }

    [Fact]
    public async Task ConcurrentAccess_SameFile_MultipleWrites_ShouldMaintainConsistency()
    {
        // Arrange
        var key = "concurrent_writes.txt";
        var numberOfWrites = 20;
        var tasks = new List<Task<Result>>();

        // Act
        for (int i = 0; i < numberOfWrites; i++)
        {
            var content = $"Content {i}";
            // Создаем новый MemoryStream для каждой операции
            var contentBytes = Encoding.UTF8.GetBytes(content);
            tasks.Add(Task.Run(async () =>
            {
                using var stream = new MemoryStream(contentBytes);
                return await _repository.PutFileAsync(key, stream);
            }));        }

        await Task.WhenAll(tasks);

        // Assert
        var finalContent = await _repository.GetFileAsync(key);
        Assert.True(!finalContent.IsError);
        Assert.NotNull(finalContent.Value);
        Assert.All(tasks, t => Assert.True(!t.Result.IsError));
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_ShouldDeleteSuccessfully()
    {
        // Arrange
        var key = "test_delete.txt";
        var content = "Test content";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, key), content);

        // Act
        var result = await _repository.DeleteFileAsync(key);

        // Assert
        Assert.True(!result.IsError);
        Assert.False(File.Exists(Path.Combine(_testDirectory, key)));
    }
    
}
