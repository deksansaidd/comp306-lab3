using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Models;

public partial class LearningPortalContext : DbContext
{
    public LearningPortalContext()
    {
    }

    public LearningPortalContext(DbContextOptions<LearningPortalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<LearningResource> LearningResources { get; set; }

    public virtual DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_Name").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.HasKey(e => e.ResourceId);

            entity.HasIndex(e => e.CreatedDate, "IX_Resources_CreatedDate").IsDescending();

            entity.HasIndex(e => e.ViewCount, "IX_Resources_ViewCount").IsDescending();

            entity.Property(e => e.ResourceId).HasColumnName("ResourceID");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.S3key)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("S3Key");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Author).WithMany(p => p.LearningResources)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resources_Author");

            entity.HasOne(d => d.Category).WithMany(p => p.LearningResources)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resources_Category");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
