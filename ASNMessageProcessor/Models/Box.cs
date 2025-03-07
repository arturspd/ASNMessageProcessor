namespace ASNMessageProcessor.Models
{
    public class Box
    {
        public string? SupplierId { get; set; }

        public string? BoxId { get; set; }

        public IReadOnlyCollection<BoxContent>? Contents { get; set; }
    }
}
