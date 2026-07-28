using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Models.Entity;

[Table("GeMBidExtracts")]
[Index("BidEndDateTime", Name = "IX_GeMBidExtracts_BidEndDateTime")]
[Index("BidEndDateTime", "CreatedOn", Name = "IX_GeMBidExtracts_BidEndDateTime_CreatedOn")]
[Index("CategoryKey", Name = "IX_GeMBidExtracts_CategoryKey")]
[Index("CreatedOn", Name = "IX_GeMBidExtracts_CreatedOn")]
[Index("DepartmentName", Name = "IX_GeMBidExtracts_DepartmentName")]
[Index("EmdAmount", Name = "IX_GeMBidExtracts_EmdAmount")]
[Index("ItemCategory", Name = "IX_GeMBidExtracts_ItemCategory")]
[Index("Ministry", Name = "IX_GeMBidExtracts_Ministry")]
[Index("OrganisationName", Name = "IX_GeMBidExtracts_OrganisationName")]
[Index("CategorySubKey", Name = "IX_gembidextracts_CategorySubKey")]
public partial class GeMbidExtract
{

    public string? PdfUrl { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? BidEndDateTime { get; set; }

    public DateTime? BidOpeningDateTime { get; set; }

    public int? BidValidityDays { get; set; }

    public string? TypeOfBid { get; set; }

    public string? EvaluationMethod { get; set; }

    public string? Ministry { get; set; }

    public string? DepartmentName { get; set; }

    public string? OrganisationName { get; set; }

    public string? OfficeName { get; set; }

    public string? ContactDetailsOfGrievanceRedressal { get; set; }

    public int? TotalQuantity { get; set; }

    public string? ItemCategory { get; set; }

    [Column("BOQTitle")]
    public string? Boqtitle { get; set; }

    public string? PrimaryProductCategory { get; set; }

    public string? SimilarCategory { get; set; }

    public string? ContractPeriod { get; set; }

    public string? MinimumAverageAnnualTurnover { get; set; }

    [Column("OEMAverageTurnover")]
    public string? OemaverageTurnover { get; set; }

    public string? YearsOfPastExperienceRequired { get; set; }

    public string? PastExperienceRequired { get; set; }

    public string? DocumentRequiredFromSeller { get; set; }

    public int? MinimumNumberOfBidsRequiredToDisableAutomaticBidExtension { get; set; }

    public int? NumberOfDaysForWhichBidWouldBeAutoExtended { get; set; }

    public int? NumberOfAutoExtensionCount { get; set; }

    [Column("BidToRAEnabled")]
    public bool? BidToRaenabled { get; set; }

    [Column("RAQualificationRule")]
    public string? RaqualificationRule { get; set; }

    public bool? InspectionRequired { get; set; }

    public bool? InspectionByBuyerOwnAgency { get; set; }

    public string? InspectionType { get; set; }

    public string? InspectionAgency { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? EstimatedBidValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? EmdAmount { get; set; }

    [Column("EPBGPercentage", TypeName = "decimal(18, 2)")]
    public decimal? Epbgpercentage { get; set; }

    [Column("EPBGDurationMonths")]
    public int? EpbgdurationMonths { get; set; }

    public string? AdvisoryBank { get; set; }

    [Column("MSEPurchasePreference")]
    public bool? MsepurchasePreference { get; set; }

    [Column("MIIPurchasePreference")]
    public bool? MiipurchasePreference { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? PurchasePreferencePercentage { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MaximumPurchasePreferencePercentage { get; set; }

    public string? ArbitrationClause { get; set; }

    public string? MediationClause { get; set; }

    public string? Specification { get; set; }

    public string? SpecificationParameterName { get; set; }

    public string? Values { get; set; }

    public string? PhysicalCharacteristics { get; set; }

    public string? Material { get; set; }

    public string? Surface { get; set; }

    public string? Layers { get; set; }

    public string? WarrantyText { get; set; }

    public string? ServiceRequirement { get; set; }

    public string? ServiceInclusions { get; set; }

    public string? TrainingModule { get; set; }

    [Column("GeMARPTSSearchedStrings")]
    public string? GeMarptssearchedStrings { get; set; }

    [Column("GeMARPTSSearchedResults")]
    public string? GeMarptssearchedResults { get; set; }

    public string? RelevantCategoriesSelectedForNotification { get; set; }

    public string? ConsigneeName { get; set; }

    public string? ConsigneeAddress { get; set; }

    public string? ConsigneeQuantity { get; set; }

    public string? TechnicalSpecificationJson { get; set; }

    public string? JsonData { get; set; }

    public string? PastPerformance { get; set; }

    public string? PaymentTimelines { get; set; }

    [Column("AutoCRACDays")]
    public string? AutoCracdays { get; set; }

    public string? FinancialDocumentRequired { get; set; }

    public string? Required { get; set; }

    [Column("ITCAvailableToBuyer")]
    public string? ItcavailableToBuyer { get; set; }

    [Column("MIICompliance")]
    public string? Miicompliance { get; set; }

    public string? BuyerSpecificationDocument { get; set; }

    [Column("BOQDetailDocument")]
    public string? BoqdetailDocument { get; set; }

    public string? SpecificationDocument { get; set; }

    public DateTime? BidDate { get; set; }

    public string? CategoryKey { get; set; }

    public string? CategorySubKey { get; set; }

    public string? CardDepartment { get; set; }

    public DateTime? CardEndDate { get; set; }

    public string? CardItemName { get; set; }

    public string? CardMinistry { get; set; }

    public int? CardQuantity { get; set; }

    public DateTime? CardStartDate { get; set; }
}
