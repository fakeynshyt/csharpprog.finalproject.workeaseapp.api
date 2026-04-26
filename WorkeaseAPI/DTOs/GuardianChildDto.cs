namespace WorkeaseAPI.DTOs
{
    public class GuardianChildDto
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public List<HealthSummaryDto> ChildHealthHistory { get; set; } = new();
        public List<FeeSummaryDto> ChildFeeHistory { get; set; } = new();
    }
}
