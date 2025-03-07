using ASNMessageProcessor.Context;
using ASNMessageProcessor.Models.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASNMessageProcessor.Services
{
    public class FileWatcherService : BackgroundService
    {
        private readonly ILogger<FileWatcherService> _logger;
        private readonly FileSystemWatcher _fileWatcher;
        private readonly IFileParser _fileParser;
        private readonly AppDbContext _dbContext;
        private readonly string? _filePath;

        public FileWatcherService(
            ILogger<FileWatcherService> logger,
            IFileParser fileParser,
            IOptions<FileWatcherSettings> options,
            AppDbContext dbContext)
        {
            _logger = logger;
            _fileParser = fileParser;
            _filePath = options.Value.FilePath;
            _dbContext = dbContext;

            if (!Directory.Exists(_filePath))
            {
                _logger.LogInformation($"Directory not found. Creating directory: {_filePath}");
                Directory.CreateDirectory(_filePath);
            }

            _fileWatcher = new FileSystemWatcher(_filePath)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.txt"
            };
            _fileWatcher.Created += async (sender, e) => await OnFileCreatedAsync(sender, e);
        }

        private async Task OnFileCreatedAsync(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"New file detected: {e.Name}");

            await Task.Delay(2000);
            await ProcessFileAsync(e.FullPath);
        }

        private async Task ProcessFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    return;
                }

                _logger.LogInformation($"Processing file: {filePath}");

                var result = await _fileParser.ParseFileAsync(filePath);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing file: {filePath}");
                throw;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _fileWatcher.Dispose();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"File watcher service started. Watching {_filePath}");
            await RecoverFilesAsync(_filePath);
        }

        // Recover files that were not processed before the service started (in case of service restart)
        private async Task RecoverFilesAsync(string filePath)
        {
            var files = Directory.GetFiles(filePath, "*.txt");
            var processedFiles = await _dbContext.ProcessedFiles.Select(x => x.FileName).ToListAsync();

            var unprocessedFiles = files.Where(fp => !processedFiles.Contains(fp)).ToList();

            foreach (var file in unprocessedFiles)
            {
                _logger.LogInformation($"Recovering file: {file}");
                await ProcessFileAsync(file);
            }
        }
    }
}
