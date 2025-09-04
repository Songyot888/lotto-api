using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api_lotto.DTOs.auth
{
    public class RegisterDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}