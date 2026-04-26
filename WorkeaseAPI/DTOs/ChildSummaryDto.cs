namespace WorkeaseAPI.DTOs
{
    public class ChildSummaryDto
    {
        public int ChildId { get; set; }
        public string ChildFullName { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public bool HasParent { get; set; }
    }
}
