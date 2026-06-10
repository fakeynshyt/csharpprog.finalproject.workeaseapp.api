using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class UpdateAttendanceDto
    {
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
    }
}
