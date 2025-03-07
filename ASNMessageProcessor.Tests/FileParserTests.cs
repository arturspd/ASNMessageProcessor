using ASNMessageProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ASNMessageProcessor.Tests
{
    public class FileParserTests
    {
        private readonly Mock<IDataInserter> _dataInserterMock;
        private readonly Mock<ILogger<FileParser>> _loggerMock;
        private readonly FileParser _fileParser;

        public FileParserTests()
        {
            _dataInserterMock = new Mock<IDataInserter>();
            _loggerMock = new Mock<ILogger<FileParser>>();
            _fileParser = new FileParser(
                _dataInserterMock.Object,
                _loggerMock.Object);
        }

        [Fact(DisplayName = "Should parse single box")]
        public async Task FileParser_ShouldParseSingleBox()
        {
            // Arrange
            var fileContent = @"
            HDR  TRSP117  6874454I                           
            LINE P000001661  9781465121550  12     
            LINE P000001661  9925151267712  2";

            var filePath = CreateTempFile(fileContent);

            // Act
            var result = await _fileParser.ParseFileAsync(filePath);

            // Assert
            Assert.Single(result);
            Assert.Equal("TRSP117", result[0].SupplierId);
            Assert.Equal("6874454I", result[0].BoxId);
            Assert.Equal(2, result[0].Contents?.Count);
            Assert.Equal("P000001661", result[0].Contents?.ToList()[0].PoNumber);
            Assert.Equal(12, result[0].Contents?.ToList()[0].Quantity);

            DeleteTempFile(filePath);
        }

        [Fact(DisplayName = "Should parse multiple boxes")]
        public async Task FileParser_ShouldParseMultipleBoxes()
        {
            // Arrange
            var fileContent = @"
            HDR  TRSP117  6874454I                           
            LINE P000001661  9781465121550  12
            HDR  TRSP118  7895123J      
            LINE P000001663  9925151267712  2";

            var filePath = CreateTempFile(fileContent);

            // Act
            var result = await _fileParser.ParseFileAsync(filePath);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("TRSP117", result[0].SupplierId);
            Assert.Single(result[0].Contents);
            Assert.Equal("TRSP118", result[1].SupplierId);
            Assert.Single(result[1].Contents);

            DeleteTempFile(filePath);
        }

        [Fact(DisplayName = "Should return empty list when file is empty")]
        public async Task FileParser_ShouldReturnEmptyListWhenFileIsEmpty()
        {
            // Arrange
            var fileContent = string.Empty;
            var filePath = CreateTempFile(fileContent);

            // Act
            var result = await _fileParser.ParseFileAsync(filePath);

            // Assert
            Assert.Empty(result);
            DeleteTempFile(filePath);
        }

        [Fact(DisplayName = "Should handle file with incorrect format")]
        public async Task FileParser_ShouldHandleFileWithIncorrectFormat()
        {
            // Arrange
            string fileContent = @"
            HDR  TRSP117  6874454I
            LINE P000001661
            LINE 9925151267712  2";

            var filePath = CreateTempFile(fileContent);

            // Act
            var result = await _fileParser.ParseFileAsync(filePath);

            // Assert
            Assert.Single(result);
            Assert.Empty(result[0].Contents);

            DeleteTempFile(filePath);
        }

        private static string CreateTempFile(string content)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private static void DeleteTempFile(string filePath)
        {
            File.Delete(filePath);
        }
    }
}
