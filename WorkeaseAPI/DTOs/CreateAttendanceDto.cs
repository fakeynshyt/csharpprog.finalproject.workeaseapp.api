using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class CreateAttendanceDto
    {
        public int ChildId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
    }
}
