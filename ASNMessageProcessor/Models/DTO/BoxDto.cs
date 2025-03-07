namespace ASNMessageProcessor.Models.DTO
{
    public class BoxDto
    {
        public string? SupplierId { get; set; }

        public string? BoxId { get; set; }

        public IReadOnlyCollection<BoxContentDto>? Contents { get; set; }
    }
}
