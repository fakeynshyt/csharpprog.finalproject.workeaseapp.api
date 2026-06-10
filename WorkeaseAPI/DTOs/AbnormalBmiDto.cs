namespace WorkeaseAPI.DTOs
{
    public class AbnormalBmiDto
    {
        public int ChildId { get; set; }
        public string ChildFirstName { get; set; } = string.Empty;
        public string ChildLastName { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public decimal WeightKg { get; set; }
        public decimal HeightCm { get; set; }
        public decimal Bmi { get; set; }
        public string BmiStatus { get; set; } = string.Empty;
    }
}
