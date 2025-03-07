using ASNMessageProcessor.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace ASNMessageProcessor.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<BoxDto> Boxes { get; set; }
        public DbSet<BoxContentDto> BoxContents { get; set; }
        public DbSet<ProcessedFileDto> ProcessedFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BoxDto>().HasKey(b => b.BoxId);
            modelBuilder.Entity<BoxContentDto>().HasKey(bc => new { bc.PoNumber, bc.Isbn });
            modelBuilder.Entity<ProcessedFileDto>().HasKey(pf => pf.FileName);

            modelBuilder.Entity<BoxDto>()
                .HasMany(b => b.Contents)
                .WithOne()
                .HasForeignKey("BoxId");
        }
    }
}
