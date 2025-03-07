using ASNMessageProcessor.Models;

namespace ASNMessageProcessor.Services
{
    public interface IDataInserter
    {
        Task InsertDataAsync(List<Box> boxes, string fileName);
    }
}
