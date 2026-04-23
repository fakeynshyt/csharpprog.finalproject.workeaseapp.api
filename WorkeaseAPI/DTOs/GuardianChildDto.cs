namespace WorkeaseAPI.DTOs
{
    public class GuardianChildDto
    {
        public int GuardianId { get; set; }
        public string GuardianFullName { get; set; } = string.Empty;
        public DateTime GuardianBirthDate { get; set; }
        public string GuardianGender { get; set; } = string.Empty;
        public string CdwCenterName { get; set; } = string.Empty;
        public List<HealthSummaryDto> ChildHealthHistory { get; set; } = new();
        public List<FeeSummaryDto> ChildFeeHistory { get; set; } = new();
    }
}
