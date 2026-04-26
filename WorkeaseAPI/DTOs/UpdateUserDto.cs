using System.ComponentModel.DataAnnotations;

namespace WorkeaseAPI.DTOs
{
    public class UpdateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public int? CenterId { get; set; } = null;
        public bool UserIsActive { get; set; } = true;
    }
}
