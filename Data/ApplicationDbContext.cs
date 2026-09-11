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

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<PuneBidAlertSent> PuneBidAlertSents { get; set; }
    public virtual DbSet<PuneBidAlertState> PuneBidAlertStates { get; set; }

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