namespace WorkeaseAPI.DTOs
{
    public class ChildReadDto
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public string CdwCenterName { get; set; } = string.Empty;
    }
}
