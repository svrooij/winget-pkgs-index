using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace WingetIndexGenerator.Models;

public partial class WingetContext : DbContext
{
    public WingetContext(string sqliteConnectionString) : this(new DbContextOptionsBuilder<WingetContext>()
        .UseSqlite(sqliteConnectionString)
        .Options)
    {

    }
    internal WingetContext(DbContextOptions<WingetContext> options) : base(options)
    {
    }

    public DbSet<Package> Packages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Package>(entity =>
        {
            entity.ToTable("packages");
            entity.HasKey(p => p.Rowid);
            entity.Property(e => e.Rowid).HasColumnName("rowid").ValueGeneratedOnAdd();

            // Connect the tags through the tags2_map table, without adding the TagsMap entity to the model
            // This is a many-to-many relationship, so we use the Fluent API to configure it
            entity.HasMany(p => p.Tags)
                .WithMany(t => t.Packages)
                .UsingEntity<TagsMap>(j =>
                {
                    j.ToTable("tags2_map");
                    j.HasKey(tm => new { tm.PackageId, tm.TagId });
                    j.Property(tm => tm.PackageId).HasColumnName("package");
                    j.Property(tm => tm.TagId).HasColumnName("tag");
                });
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags2");
            entity.HasKey(t => t.Rowid);
            entity.Property(t => t.TagValue).HasColumnName("tag");
            entity.Property(e => e.Rowid).ValueGeneratedOnAdd();
        });
    }

    public override void Dispose()
    {
        // Clear the connection pool to avoid locking issues when disposing the context
        // This is specific to Sqlite, other providers may have different methods
        if (Database.GetDbConnection() is SqliteConnection sqliteConnection)
        {
            SqliteConnection.ClearPool(sqliteConnection);
        }
        base.Dispose();
    }
}