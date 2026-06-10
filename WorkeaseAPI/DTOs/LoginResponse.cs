namespace WorkeaseAPI.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
