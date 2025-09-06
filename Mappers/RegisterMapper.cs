using api_lotto.DTOs.auth;
using lotto_api.Models;

namespace api_lotto.Mappers
{
    public static class RegisterMapper
    {
        public static User ToRegister(this RegisterDTO dto, string hashedPassword)
        {
            return new User
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Balance = dto.Balance,
                BankName = dto.BankName,
                BankNumber = dto.BankNumber,
                Password = hashedPassword,
            };
        }

        public static AuthRegisterResponseDTO ToAuthRegisterResponse(this User u) =>
           new AuthRegisterResponseDTO
           {
               Uid = (int)u.Uid,
               FullName = u.FullName,
               Email = u.Email,
               BankName = u.BankName,
               BankNumber = u.BankNumber
           };
    }
}