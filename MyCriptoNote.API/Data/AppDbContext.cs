using Microsoft.EntityFrameworkCore;
using MyCriptoNote.API.Models;

namespace MyCriptoNote.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(100);
            entity.Property(f => f.PasswordHash).IsRequired();
            entity.HasIndex(f => f.UserId);

            entity.HasOne(f => f.User)
                  .WithMany(u => u.Folders)
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.EncryptedContent).IsRequired();
            entity.Property(n => n.Salt).IsRequired();
            entity.Property(n => n.IV).IsRequired();
            entity.Property(n => n.AuthTag).IsRequired(false);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.FolderId);

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notes)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Folder)
                  .WithMany(f => f.Notes)
                  .HasForeignKey(n => n.FolderId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired(false);
        });
    }
}
