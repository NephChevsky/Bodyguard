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
        public DbSet<Token> Tokens => Set<Token>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

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

                AddGenericFields<TwitchStreamer>(entity);
            });
            modelBuilder.Entity<TwitchStreamer>().HasIndex(t => new { t.Id }).IsUnique(true);
            modelBuilder.Entity<TwitchStreamer>().HasIndex(t => new { t.Name }).HasFilter($"{nameof(TwitchStreamer.Deleted)} = 0").IsUnique(true);

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
                  .HasMaxLength(512)
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