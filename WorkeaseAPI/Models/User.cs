namespace WorkeaseAPI.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserHashPassword { get; set; } = string.Empty;
        public DateTime UserBirthDate { get; set; }
        public string UserGender { get; set; } = string.Empty;
        public string UserAddress { get; set; } = string.Empty;
        public string UserContactNo {  get; set; } = string.Empty;
        public string UserType { get; set; } = "Admin"; // "Admin", "Worker", "Child or Parent"
        public int UserCDWCenterId { get; set; }
        public bool UserIsActive { get; set; } = true;
    }
}
