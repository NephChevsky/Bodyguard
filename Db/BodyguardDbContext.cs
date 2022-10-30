using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Models.Db;
using Models.Interfaces;
using System.Linq.Expressions;

namespace Db
{
	public partial class BodyguardDbContext : DbContext
	{
        public BodyguardDbContext(DbContextOptions options) : base(options)
        {
        }

        public BodyguardDbContext()
        {
        }

        public DbSet<TwitchStreamer> TwitchStreamers => Set<TwitchStreamer>();
        public DbSet<TwitchViewer> TwitchViewers => Set<TwitchViewer>();
        public DbSet<TwitchMessage> TwitchMessages => Set<TwitchMessage>();
        public DbSet<TwitchBan> TwitchBans => Set<TwitchBan>();
        public DbSet<TwitchTimeout> TwitchTimeouts => Set<TwitchTimeout>(); 
        public DbSet<Token> Tokens => Set<Token>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            string path = "secret.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Bodyguard", path);
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false)
                .Build();

            var connectionString = configuration.GetConnectionString("DbKey");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TwitchStreamer>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(512);

                AddGenericFields<TwitchStreamer>(entity);
            });
            modelBuilder.Entity<TwitchStreamer>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<TwitchViewer>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(512);

                AddGenericFields<TwitchViewer>(entity);
            });
            modelBuilder.Entity<TwitchViewer>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<TwitchMessage>(entity =>
            {
                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Channel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Sentiment)
                    .IsRequired()
                    .HasDefaultValue(null);

                entity.Property(e => e.SentimentScore)
                    .IsRequired()
                    .HasDefaultValue(0);

                AddGenericFields<TwitchMessage>(entity);
            });
            modelBuilder.Entity<TwitchMessage>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<TwitchBan>(entity =>
            {
                entity.Property(e => e.Channel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.BanReason)
                    .HasMaxLength(512);

                AddGenericFields<TwitchBan>(entity);
            });
            modelBuilder.Entity<TwitchBan>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<TwitchTimeout>(entity =>
            {
                entity.Property(e => e.Channel)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TimeoutDuration)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.TimeoutReason)
                    .HasMaxLength(512);

                AddGenericFields<TwitchTimeout>(entity);
            });
            modelBuilder.Entity<TwitchTimeout>().HasIndex(t => new { t.Id }).IsUnique(true);

            Expression<Func<ISoftDeleteable, bool>> filterSoftDeleteable = bm => !bm.Deleted;
            Expression? filter = null;
            foreach (var type in modelBuilder.Model.GetEntityTypes())
            {
                var param = Expression.Parameter(type.ClrType, "entity");
                if (typeof(ISoftDeleteable).IsAssignableFrom(type.ClrType))
                {
                    filter = AddFilter(filter, ReplacingExpressionVisitor.Replace(filterSoftDeleteable.Parameters.First(), param, filterSoftDeleteable.Body));
                }

                if (filter != null)
                {
                    type.SetQueryFilter(Expression.Lambda(filter, param));
                }
            }
        }

        private Expression AddFilter(Expression? filter, Expression newFilter)
        {
            if (filter == null)
            {
                filter = newFilter;
            }
            else
            {
                filter = Expression.And(filter, newFilter);
            }
            return filter;
        }

        public void AddGenericFields<T>(EntityTypeBuilder entity)
        {
            entity.Property("Id")
                  
                  .IsRequired();

            if (typeof(ISoftDeleteable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("Deleted")
                    .IsRequired()
                    .HasDefaultValue(false);
            }

            if (typeof(IDateTimeTrackable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("CreationDateTime")
                   .IsRequired();

                entity.Property("LastModificationDateTime");
            }

            if (typeof(ITwitchOwnable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("TwitchOwner")
                    .HasMaxLength(20)
                    .IsRequired();
            }
        }

        public override int SaveChanges()
        {
            SoftDelete();
            TimeTrack();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SoftDelete();
            TimeTrack();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void SoftDelete()
        {
            ChangeTracker.DetectChanges();
            var markedAsDeleted = ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted);
            foreach (var item in markedAsDeleted)
            {
                if (item.Entity is ISoftDeleteable entity)
                {
                    item.State = EntityState.Unchanged;
                    entity.Deleted = true;
                }
            }
        }

        private void TimeTrack()
        {
            ChangeTracker.DetectChanges();
            var markedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
            foreach (var item in markedEntries)
            {
                if (item.Entity is IDateTimeTrackable entity)
                {
                    entity.LastModificationDateTime = now;
                    if (item.State == EntityState.Added && entity.CreationDateTime == DateTime.MinValue)
                    {
                        entity.CreationDateTime = now;
                    }
                }
            }
        }
    }
}