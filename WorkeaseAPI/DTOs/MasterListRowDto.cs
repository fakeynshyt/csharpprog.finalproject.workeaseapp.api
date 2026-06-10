namespace WorkeaseAPI.DTOs
{
    public class MasterListRowDto
    {
        public int RowNumber { get; set; }
        public int ChildId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty; // ✅ added
        public DateTime BirthDate { get; set; }
        public int AgeInMonths { get; set; }
        public decimal WeightKg { get; set; }
        public decimal HeightCm { get; set; }
        public DateTime? LastWeighDate { get; set; }
        public string Guardian { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
