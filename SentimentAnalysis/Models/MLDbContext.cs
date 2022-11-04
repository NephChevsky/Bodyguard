using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using SentimentAnalysis.Models.Db;
using System.Linq.Expressions;

namespace SentimentAnalysis
{
    public partial class MLDbContext : DbContext
    {
        public MLDbContext(DbContextOptions options) : base(options)
        {
        }

        public MLDbContext()
        {
        }

        public DbSet<TwitchSample> TwitchSamples => Set<TwitchSample>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            string path = "secret.json";
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false)
                .Build();

            var connectionString = configuration.GetConnectionString("MLDbKey");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TwitchSample>(entity =>
            {
                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Sentiment)
                    .IsRequired();

                entity.Property(e => e.CreationDateTime)
                    .IsRequired();
            });
            modelBuilder.Entity<TwitchSample>().HasIndex(t => new { t.Id }).IsUnique(true);
        }
    }
}