using ASNMessageProcessor.Models;

namespace ASNMessageProcessor.Services
{
    public interface IFileParser
    {
        Task<List<Box>> ParseFileAsync(string filePath);
    }
}
