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
        public string ChildGuardianName { get; set; } = string.Empty;
        public string ChildGuardianContactNo { get; set; } = string.Empty;
        public int ChildCDWCenterId { get; set; }
        public Center? Center { get; set; }
        public DateTime CHildEnrolledDate { get; set; }
        public DateTime CHildUpdatedDate { get; set; }
    }
}
