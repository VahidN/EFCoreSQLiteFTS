using System.Threading;
using System.Threading.Tasks;
using EFCoreSQLiteFTS.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCoreSQLiteFTS.DataLayer
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(LoggerFactory.Create(x => x
                    .AddConsole()
                    .AddFilter(y => y >= LogLevel.Debug)))
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ChapterFTS>().HasNoKey().ToView(null);
            builder.Entity<SpellCheck>().HasNoKey().ToView(null);
        }

        public override int SaveChanges()
        {
            var changedChapters = this.GetChangedEntities<Chapter>();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = base.SaveChanges();
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            this.UpdateChapterFTS(changedChapters);
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var changedChapters = this.GetChangedEntities<Chapter>();

            this.ChangeTracker.AutoDetectChangesEnabled = false; // for performance reasons, to avoid calling DetectChanges() again.
            var result = await base.SaveChangesAsync(cancellationToken);
            this.ChangeTracker.AutoDetectChangesEnabled = true;

            this.UpdateChapterFTS(changedChapters);
            return result;
        }
    }
}