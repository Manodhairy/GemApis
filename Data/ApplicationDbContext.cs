using System;
using System.Collections.Generic;
using GemApi.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BidNotificationState> BidNotificationStates { get; set; }

    public virtual DbSet<GeMbidExtract> GeMbidExtracts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=103.185.74.188;Database=ETender_trainee;User Id=trainee;Password=StrongP@ssw0rd123!;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BidNotificationState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BidNotif__3214EC073FCD4301");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<GeMbidExtract>(entity =>
        {
            entity.HasIndex(e => e.BidNumber, "IX_GeMBidExtracts_BidNumber")
                .IsUnique()
                .HasFilter("([BidNumber] IS NOT NULL)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
