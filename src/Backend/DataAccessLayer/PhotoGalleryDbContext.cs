using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.DataAccessLayer.Entities;

namespace PhotoGallery.DataAccessLayer;

public class PhotoGalleryDbContext : DbContext
{
    public DbSet<Photo> Photos { get; set; } = null!;

    public DbSet<Comment> Comments { get; set; } = null!;

    public PhotoGalleryDbContext(DbContextOptions<PhotoGalleryDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
