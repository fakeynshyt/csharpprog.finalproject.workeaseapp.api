namespace WorkeaseAPI.Models
{
    public class HealthRecord
    {
        public int HealthRecordId { get; set; }
        public int ChildId { get; set; }
        public Child? Child { get; set; }
        public DateTime HealthRecordDate { get; set; }
        public decimal HealthRecordWeightKg { get; set; }
        public decimal HealthRecordHeightCm { get; set; }
        public decimal HealthRecordBmi => CalculateBMI(); // Asian Based BMI
        public string HealthRecordNotes { get; set; } = string.Empty;
        public int HealthRecordedByUserId { get; set; }
        public User? RecordedByUser { get; set; }
        public bool HealthRecordIsSync { get; set; } = false;
        public DateTime HealthRecordCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime HealthRecordUpdatedAt { get; set; } = DateTime.UtcNow;
        private decimal CalculateBMI()
        {
            if (HealthRecordHeightCm <= 0 || HealthRecordWeightKg <= 0) return 0;

            var heightInMeters = HealthRecordHeightCm / 100;
            return (decimal)Math.Round(HealthRecordWeightKg / (heightInMeters * heightInMeters), 2);
        }
    }
}
