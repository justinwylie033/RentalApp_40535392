using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Rental> Rentals => Set<Rental>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Presentation point: EF Core is the single source of truth for relational
        // constraints, precision, enum conversion, relationships, and spatial types.
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.DisplayName).HasMaxLength(80);
            entity.Property(user => user.Email).HasMaxLength(254);
            entity.Property(user => user.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.Property(token => token.TokenHash).HasMaxLength(128);
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(120);
            entity.Property(item => item.Description).HasMaxLength(1_500);
            entity.Property(item => item.Address).HasMaxLength(250);
            entity.Property(item => item.DailyRate).HasPrecision(10, 2);
            entity.Property(item => item.Category).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.Location).HasColumnType("geography (point)");
            // GiST is the spatial index used by PostGIS to accelerate radius searches.
            entity.HasIndex(item => item.Location).HasMethod("gist");
            entity.HasOne(item => item.Owner)
                .WithMany(user => user.Items)
                .HasForeignKey(item => item.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(rental => rental.Id);
            entity.Property(rental => rental.TotalPrice).HasPrecision(10, 2);
            entity.Property(rental => rental.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(rental => new { rental.ItemId, rental.StartDateUtc, rental.EndDateUtc });
            entity.HasOne(rental => rental.Item)
                .WithMany(item => item.Rentals)
                .HasForeignKey(rental => rental.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rental => rental.Borrower)
                .WithMany(user => user.Rentals)
                .HasForeignKey(rental => rental.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(review => review.Id);
            entity.HasIndex(review => review.RentalId).IsUnique();
            entity.Property(review => review.Comment).HasMaxLength(1_000);
            // Database constraints remain a final integrity boundary even if API
            // validation is accidentally bypassed.
            entity.ToTable(table => table.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            entity.HasOne(review => review.Rental)
                .WithOne(rental => rental.Review)
                .HasForeignKey<Review>(review => review.RentalId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(review => review.Item)
                .WithMany(item => item.Reviews)
                .HasForeignKey(review => review.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(review => review.Reviewer)
                .WithMany(user => user.Reviews)
                .HasForeignKey(review => review.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
