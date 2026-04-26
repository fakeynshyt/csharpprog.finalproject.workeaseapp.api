namespace WorkeaseAPI.DTOs
{
    public class CreateChildDto
    {
        public string ChildFirstName { get; set; } = string.Empty;
        public string ChildLastName { get; set; } = string.Empty;
        public DateTime ChildBirthDate { get; set; }
        public string ChildGender { get; set; } = string.Empty;
        public int CenterId { get; set; }
        public int? UserId { get; set; } // optional parent link
    }
}
