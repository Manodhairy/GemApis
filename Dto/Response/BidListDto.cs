namespace GemApi.DTOs.Response
{
    public class BidListDto
    {
        public string? BidNumber { get; set; }

        public string? PdfUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? BidEndDateTime { get; set; }

        public DateTime? BidOpeningDateTime { get; set; }

        public bool IsClosingSoon { get; set; }

        public bool IsActive {  get; set; }

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

        public string? Boqtitle { get; set; }

        public string? PrimaryProductCategory { get; set; }

        public string? SimilarCategory { get; set; }

        public string? ContractPeriod { get; set; }

        public string? MinimumAverageAnnualTurnover { get; set; }

        public string? OemaverageTurnover { get; set; }

        public string? YearsOfPastExperienceRequired { get; set; }

        public string? PastExperienceRequired { get; set; }

        public string? DocumentRequiredFromSeller { get; set; }

        public int? MinimumNumberOfBidsRequiredToDisableAutomaticBidExtension { get; set; }

        public int? NumberOfDaysForWhichBidWouldBeAutoExtended { get; set; }

        public int? NumberOfAutoExtensionCount { get; set; }

        public bool? BidToRaenabled { get; set; }

        public string? RaqualificationRule { get; set; }

        public bool? InspectionRequired { get; set; }

        public bool? InspectionByBuyerOwnAgency { get; set; }

        public string? InspectionType { get; set; }

        public string? InspectionAgency { get; set; }

        public decimal? EstimatedBidValue { get; set; }

        public decimal? EmdAmount { get; set; }

        public decimal? Epbgpercentage { get; set; }

        public int? EpbgdurationMonths { get; set; }

        public string? AdvisoryBank { get; set; }

        public bool? MsepurchasePreference { get; set; }

        public bool? MiipurchasePreference { get; set; }

        public decimal? PurchasePreferencePercentage { get; set; }

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

        public string? GeMarptssearchedStrings { get; set; }

        public string? GeMarptssearchedResults { get; set; }

        public string? RelevantCategoriesSelectedForNotification { get; set; }

        public string? ConsigneeName { get; set; }

        public string? ConsigneeAddress { get; set; }

        public string? ConsigneeQuantity { get; set; }

        public string? TechnicalSpecificationJson { get; set; }


        public string? PastPerformance { get; set; }

        public string? PaymentTimelines { get; set; }

        public string? AutoCracdays { get; set; }

        public string? FinancialDocumentRequired { get; set; }

        public string? Required { get; set; }

        public string? ItcavailableToBuyer { get; set; }

        public string? Miicompliance { get; set; }

        public string? BuyerSpecificationDocument { get; set; }

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
}