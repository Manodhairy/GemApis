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

    public virtual DbSet<GeMbidExtract> GeMbidExtracts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=103.185.74.188;Database=ETender_trainee;User Id=trainee;Password=StrongP@ssw0rd123!;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeMbidExtract>(entity =>
        {
            entity.ToTable("GeMBidExtracts");

            entity.HasIndex(e => e.BidEndDateTime, "IX_GeMBidExtracts_BidEndDateTime");

            entity.HasIndex(e => new { e.BidEndDateTime, e.CreatedOn }, "IX_GeMBidExtracts_BidEndDateTime_CreatedOn");

            entity.HasIndex(e => e.BidNumber, "IX_GeMBidExtracts_BidNumber")
                .IsUnique()
                .HasFilter("([BidNumber] IS NOT NULL)");

            entity.HasIndex(e => e.CategoryKey, "IX_GeMBidExtracts_CategoryKey");

            entity.HasIndex(e => e.CreatedOn, "IX_GeMBidExtracts_CreatedOn");

            entity.HasIndex(e => e.DepartmentName, "IX_GeMBidExtracts_DepartmentName");

            entity.HasIndex(e => e.EmdAmount, "IX_GeMBidExtracts_EmdAmount");

            entity.HasIndex(e => e.ItemCategory, "IX_GeMBidExtracts_ItemCategory");

            entity.HasIndex(e => e.Ministry, "IX_GeMBidExtracts_Ministry");

            entity.HasIndex(e => e.OrganisationName, "IX_GeMBidExtracts_OrganisationName");

            entity.Property(e => e.AutoCracdays).HasColumnName("AutoCRACDays");
            entity.Property(e => e.BidToRaenabled).HasColumnName("BidToRAEnabled");
            entity.Property(e => e.BoqdetailDocument).HasColumnName("BOQDetailDocument");
            entity.Property(e => e.Boqtitle).HasColumnName("BOQTitle");
            entity.Property(e => e.EmdAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EpbgdurationMonths).HasColumnName("EPBGDurationMonths");
            entity.Property(e => e.Epbgpercentage)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("EPBGPercentage");
            entity.Property(e => e.EstimatedBidValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeMarptssearchedResults).HasColumnName("GeMARPTSSearchedResults");
            entity.Property(e => e.GeMarptssearchedStrings).HasColumnName("GeMARPTSSearchedStrings");
            entity.Property(e => e.ItcavailableToBuyer).HasColumnName("ITCAvailableToBuyer");
            entity.Property(e => e.MaximumPurchasePreferencePercentage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Miicompliance).HasColumnName("MIICompliance");
            entity.Property(e => e.MiipurchasePreference).HasColumnName("MIIPurchasePreference");
            entity.Property(e => e.MsepurchasePreference).HasColumnName("MSEPurchasePreference");
            entity.Property(e => e.OemaverageTurnover).HasColumnName("OEMAverageTurnover");
            entity.Property(e => e.PurchasePreferencePercentage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RaqualificationRule).HasColumnName("RAQualificationRule");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
