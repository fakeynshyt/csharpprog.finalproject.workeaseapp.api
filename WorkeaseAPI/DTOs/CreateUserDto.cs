namespace WorkeaseAPI.DTOs
{
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // "Admin" | "CDW" | "Parent"
        public string UserType { get; set; } = string.Empty;

        // null for Admin and Parent
        public int? CenterId { get; set; } = null;

        // Password goes here — validated before hashing
        public string UserHashPassword { get; set; } = string.Empty;
    }
}
