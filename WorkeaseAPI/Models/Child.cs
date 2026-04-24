namespace WorkeaseAPI.Models
{
    public class Child
    {
        public int ChildId { get; set; }
        public string ChildFirstName { get; set; } = string.Empty;
        public string ChildLastName { get; set; } = string.Empty;
        public string ChildEmail { get; set; } = string.Empty; // For parent loggin credentials
        public string ChildHashPassword { get; set; } = string.Empty;
        public DateTime ChildBirthDate {  get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public User? Guardian { get; set; }
        public string ChildGuardianContactNo { get; set; } = string.Empty;
        public int CenterId { get; set; }
        public Center? Center { get; set; }
        public DateTime ChildEnrolledDate { get; set; } = DateTime.UtcNow;
        public DateTime ChildUpdatedDate { get; set; } = DateTime.UtcNow;
        public bool ChildIsActive { get; set; } = true;
    }
}
