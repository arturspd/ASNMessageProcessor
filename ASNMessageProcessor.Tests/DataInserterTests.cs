using ASNMessageProcessor.Context;
using ASNMessageProcessor.Models;
using ASNMessageProcessor.Models.DTO;
using ASNMessageProcessor.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ASNMessageProcessor.Tests
{
    public class DataInserterTests
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<ILogger<DataInserter>> _loggerMock;
        private readonly DataInserter _dataInserter;

        public DataInserterTests()
        {
            _dbContext = TestHelper.CreateTestDbContext();
            _loggerMock = new Mock<ILogger<DataInserter>>();
            _dataInserter = new DataInserter(_dbContext, _loggerMock.Object);
        }

        [Fact(DisplayName = "Should successfully insert parsed data into database")]
        public async Task DataInserter_ShouldSuccessfullyInsertParsedDataIntoDatabase()
        {
            // Arrange
            var boxes = new List<Box>
            {
                new Box
                {
                    BoxId = "6874454I",
                    SupplierId = "TRSP117",
                    Contents = new List<BoxContent>
                    {
                        new BoxContent
                        {
                            PoNumber = "P000001661",
                            Isbn = "9781465121550",
                            Quantity = 5
                        }
                    }
                }
            };

            var fileName = "test.txt";

            // Act
            await _dataInserter.InsertDataAsync(boxes, fileName);

            // Assert
            Assert.Equal(1, _dbContext.Boxes.Count());
            Assert.Equal(1, _dbContext.BoxContents.Count());
            Assert.Equal(1, _dbContext.ProcessedFiles.Count());

            var box = _dbContext.Boxes.First();
            Assert.Equal("TRSP117", box.SupplierId);
            Assert.Equal("6874454I", box.BoxId);

            var content = _dbContext.BoxContents.First();
            Assert.Equal("P000001661", content.PoNumber);
            Assert.Equal("9781465121550", content.Isbn);
            Assert.Equal(5, content.Quantity);

            var processedFile = _dbContext.ProcessedFiles.First();
            Assert.Equal(fileName, processedFile.FileName);
        }

        [Fact(DisplayName = "Should handle exceptions during data insertion")]
        public async Task DataInserter_ShouldHandleExceptionsDuringDataInsertion()
        {
            // Arrange
            var boxes = new List<Box>
            {
                new Box
                {
                    BoxId = "6874454I",
                    SupplierId = "TRSP117",
                    Contents = new List<BoxContent>
                    {
                        new BoxContent
                        {
                            PoNumber = "P000001661",
                            Isbn = "9781465121550",
                            Quantity = 5
                        }
                    }
                }
            };
            var fileName = "test.txt";

            _dbContext.Boxes.Add(new BoxDto { BoxId = "6874454I" });
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert
                .ThrowsAsync<InvalidOperationException>(
                () => _dataInserter.InsertDataAsync(boxes, fileName));

            Assert.Contains(
                "The instance of entity type 'BoxDto' cannot be tracked", exception.Message);

            Assert.Equal(1, _dbContext.Boxes.Count());
            Assert.Equal(0, _dbContext.BoxContents.Count());
            Assert.Equal(0, _dbContext.ProcessedFiles.Count());
        }
    }
}
