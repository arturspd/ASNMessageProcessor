using ASNMessageProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ASNMessageProcessor.Services
{
    public class FileParser : IFileParser
    {
        private readonly IDataInserter _dataInserter;
        private readonly ILogger<FileParser> _logger;
        private const int BatchSize = 1000;

        public FileParser(IDataInserter dataInserter, ILogger<FileParser> logger)
        {
            _dataInserter = dataInserter;
            _logger = logger;
        }

        public async Task<List<Box>> ParseFileAsync(string filePath)
        {
            var boxes = new List<Box>();
            Box? currentBox = null;

            _logger.LogInformation($"---\nParsing {filePath}");

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                {
                    continue;
                }

                // TODO: Logic for checking whether the file/section is in the correct format
                // In case the section is missing HDR, the section should be skipped or error should be logged/exception thrown
                currentBox = ProcessParts(boxes, currentBox, parts);

                if (boxes.Count >= BatchSize)
                {
                    await _dataInserter.InsertDataAsync(boxes, filePath);
                    boxes.Clear();
                }
            }

            if (boxes.Count > 0)
            {
                await _dataInserter.InsertDataAsync(boxes, filePath);
            }

            _logger.LogInformation($"Parsed {boxes.Count} boxes.\n---");
            return boxes;
        }

        private static Box? ProcessParts(List<Box> boxes, Box? currentBox, string[] parts)
        {
            if (parts[0] == "HDR" && parts.Length >= 3)
            {
                currentBox = new Box
                {
                    SupplierId = parts[1],
                    BoxId = parts[2],
                    Contents = new List<BoxContent>()
                };
                boxes.Add(currentBox);
            }
            else if (parts[0] == "LINE" && parts.Length >= 4 && currentBox != null)
            {
                var content = new BoxContent
                {
                    PoNumber = parts[1],
                    Isbn = parts[2],
                    Quantity = int.Parse(parts[3])
                };

                var contents = currentBox.Contents?.ToList();
                contents?.Add(content);
                currentBox.Contents = contents;
            }

            return currentBox;
        }
    }
}
