using ASNMessageProcessor.Context;
using ASNMessageProcessor.Models;
using ASNMessageProcessor.Models.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ASNMessageProcessor.Services
{
    public class DataInserter : IDataInserter
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DataInserter> _logger;

        public DataInserter(AppDbContext dbContext, ILogger<DataInserter> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task InsertDataAsync(List<Box> boxes, string fileName)
        {
            if (boxes == null || boxes.Count == 0)
            {
                _logger.LogWarning("No data to insert.");
                return;
            }

            if (await _dbContext.ProcessedFiles.AnyAsync(f => f.FileName == fileName))
            {
                _logger.LogWarning($"File {fileName} has already been processed.");
                return;
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Inserting {boxes.Count} boxes.");

                CreateBoxes(boxes, out List<BoxDto> boxDtos, out List<BoxContentDto> boxContentDtos);

                await ExecuteDbOperations(fileName, transaction, boxDtos, boxContentDtos);

                _logger.LogInformation("Data inserted successfully.");
            }
            catch (Exception ex)
            {
                // What will happen when data fails to insert? Should there be some kind of retry mechanism?
                _logger.LogError(ex, "Failed to insert data.");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task ExecuteDbOperations(string fileName, IDbContextTransaction transaction, List<BoxDto> boxDtos, List<BoxContentDto> boxContentDtos)
        {
            await _dbContext.Boxes.AddRangeAsync(boxDtos);
            await _dbContext.BoxContents.AddRangeAsync(boxContentDtos);
            await _dbContext.SaveChangesAsync();

            await _dbContext.ProcessedFiles.AddAsync(new ProcessedFileDto
            {
                FileName = fileName,
                ProcessedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        private static void CreateBoxes(List<Box> boxes, out List<BoxDto> boxDtos, out List<BoxContentDto> boxContentDtos)
        {
            boxDtos = new List<BoxDto>();
            boxContentDtos = new List<BoxContentDto>();
            foreach (var box in boxes)
            {
                var boxDto = new BoxDto
                {
                    SupplierId = box.SupplierId,
                    BoxId = box.BoxId,
                    Contents = box.Contents?.Select(c => new BoxContentDto
                    {
                        PoNumber = c.PoNumber,
                        Isbn = c.Isbn,
                        Quantity = c.Quantity
                    }).ToList()
                };

                boxDtos.Add(boxDto);

                if (boxDto.Contents != null)
                {
                    boxContentDtos.AddRange(boxDto.Contents);
                }
            }
        }
    }
}
