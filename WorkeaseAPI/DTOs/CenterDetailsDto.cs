namespace WorkeaseAPI.DTOs
{
    public class CenterDetailsDto
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string CenterLocation { get; set; } = string.Empty;
        public List<CdwUserDto> CdwWorkers { get; set; } = new();
        public List<ChildSummaryDto> Children { get; set; } = new();
        public int TotalChildren { get; set; }
        public int TotalCdwWorkers { get; set; }
    }
}
