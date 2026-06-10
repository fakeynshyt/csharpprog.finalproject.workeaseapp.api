namespace WorkeaseAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserHashPassword { get; set; } = string.Empty;
        public string UserType { get; set; } = "Admin"; // "Admin", "CDW", and "Parent"
        public int? CenterId { get; set; }
        public Center? Center { get; set; }
        public bool UserIsActive { get; set; } = true;
        public DateTime UserCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UserUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
