using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class CreateHealthDto
    {
        public int ChildId { get; set; }
        public DateTime HealthRecordDate { get; set; }
        public decimal HealthRecordWeigtKg { get; set; }
        public decimal HealthRecordHeightCm { get; set; }
        public string? HealthRecordNotes { get; set; }
    }
}
