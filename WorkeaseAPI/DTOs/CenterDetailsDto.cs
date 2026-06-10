namespace WorkeaseAPI.DTOs
{
    public class CenterDetailsDto
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string CenterLocation { get; set; } = string.Empty;
        public List<string> CdwWorkers { get; set; } = new();
        public List<string> Children { get; set; } = new();
    }
}
