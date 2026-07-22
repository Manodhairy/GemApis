namespace GemApi.DTOs.Response
{
    public class DashboardDto
    {
        public int TotalBids { get; set; }
        public int ActiveBids { get; set; }
        public int ClosingSoon { get; set; }
        public int ExpiredBids { get; set; }
        public int TotalMinistries { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalOrganisations { get; set; }
        public decimal TotalEstimatedValue { get; set; }
    }
}