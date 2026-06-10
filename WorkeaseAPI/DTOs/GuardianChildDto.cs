namespace WorkeaseAPI.DTOs
{
    public class GuardianChildDto
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public string ChildAddress { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public int CenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime ChildEnrolledDate { get; set; }
        public bool ChildIsActive { get; set; }
        public DateTime ChildUpdatedDate { get; set; }
    }
}
