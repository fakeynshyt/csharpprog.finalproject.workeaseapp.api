namespace WorkeaseAPI.Models
{
    public class HealthRecord
    {
        public int HealthRecordId { get; set; }
        public int ChildId { get; set; }
        public Child? Child { get; set; }
        public DateTime HealthRecordDate { get; set; }
        public decimal HealthRecordWeigtKg { get; set; }
        public decimal HealthRecordHeightCm { get; set; }
        public decimal HealthRecordBmi => CalculateBMI(); // Asian Based BMI
        public bool HealthRecordIsPresent { get; set; }
        public string HealthRecordNotes { get; set; } = string.Empty;
        public int HealthRecordedByUserId { get; set; }
        public bool HealthRecordIsSync { get; set; } = false;
        public DateTime HealthRecordCreatedAt { get; set; } = DateTime.UtcNow;

        private decimal CalculateBMI()
        {
            if (HealthRecordHeightCm <= 0 || HealthRecordWeigtKg <= 0) return 0;

            var heightInMeters = HealthRecordHeightCm / 100;
            return (decimal)Math.Round(HealthRecordWeigtKg / (heightInMeters * heightInMeters), 2);
        }
    }
}
