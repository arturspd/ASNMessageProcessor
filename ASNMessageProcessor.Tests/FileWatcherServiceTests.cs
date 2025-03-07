using ASNMessageProcessor.Context;
using ASNMessageProcessor.Models.Configuration;
using ASNMessageProcessor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ASNMessageProcessor.Tests
{
    public class FileWatcherServiceTests
    {
        private readonly Mock<ILogger<FileWatcherService>> _loggerMock;
        private readonly IFileParser _fileParser;
        private readonly Mock<IOptions<FileWatcherSettings>> _optionsMock;
        private readonly AppDbContext _dbContext;
        private readonly string testDirectoryPath;

        public FileWatcherServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileWatcherService>>();
            _fileParser = new Mock<IFileParser>().Object;

            testDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            _optionsMock = new Mock<IOptions<FileWatcherSettings>>();
            _optionsMock
                .Setup(x => x.Value)
                .Returns(new FileWatcherSettings { FilePath = testDirectoryPath });

            _dbContext = TestHelper.CreateTestDbContext();
        }

        [Fact(DisplayName = "Should log info when new files are dropped")]
        public async Task FileWatcherService_ShouldLogInfoWhenFilesAreDropped()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            Directory.CreateDirectory(testDirectoryPath);

            try
            {
                var fileWatcher = new FileWatcherService(
                    _loggerMock.Object,
                    _fileParser,
                    _optionsMock.Object,
                    _dbContext);

                // Act
                await fileWatcher.StartAsync(cancellationTokenSource.Token);

                // Test single file
                var testFilePath1 = Path.Combine(testDirectoryPath, "testFile.txt");
                File.WriteAllText(testFilePath1, "Test file content 1");

                await Task.Delay(2500);

                // Test multiple files
                var testFilePath2 = Path.Combine(testDirectoryPath, "testFile2.txt");
                var testFilePath3 = Path.Combine(testDirectoryPath, "testFile3.txt");
                File.WriteAllText(testFilePath2, "Test file content 2");
                File.WriteAllText(testFilePath3, "Test file content 3");

                await Task.Delay(2500);

                await fileWatcher.StopAsync(cancellationTokenSource.Token);

                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()
                        .Contains("New file detected")),
                        null,
                        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                    Times.Exactly(3));
            }
            finally
            {
                Directory.Delete(testDirectoryPath, true);
                cancellationTokenSource.Dispose();
            }
        }

        [Fact(DisplayName = "Should process unprocessed files on startup")]
        public async Task FileWatcherService_ShouldProcessUnprocessedFilesOnStartup()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            Directory.CreateDirectory(testDirectoryPath);

            try
            {
                var testFilePath1 = Path.Combine(testDirectoryPath, "testFile1.txt");
                var testFilePath2 = Path.Combine(testDirectoryPath, "testFile2.txt");
                File.WriteAllText(testFilePath1, "Test file content 1");
                File.WriteAllText(testFilePath2, "Test file content 2");

                var fileWatcher = new FileWatcherService(
                    _loggerMock.Object,
                    _fileParser,
                    _optionsMock.Object,
                    _dbContext);

                // Act
                await fileWatcher.StartAsync(cancellationTokenSource.Token);

                await Task.Delay(2500);

                await fileWatcher.StopAsync(cancellationTokenSource.Token);

                // Assert
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing file")),
                        null,
                        (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                    Times.Exactly(2));
            }
            finally
            {
                Directory.Delete(testDirectoryPath, true);
                cancellationTokenSource.Dispose();
            }
        }
    }
}
