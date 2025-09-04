namespace api_lotto.DTOs.auth
{
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthLoginResponseDTO
    {
        public int Uid { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}