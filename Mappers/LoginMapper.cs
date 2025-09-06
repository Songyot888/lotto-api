using api_lotto.DTOs.auth;
using lotto_api.Models;

namespace lotto_api.Mappers
{
    public static class LoginMapper
    {
        public static User ToLogin(this LoginDTO dTO)
        {
            return new User
            {

            };
        }

        public static AuthLoginResponseDTO ToAuthLoginResponse(this User u) =>
          new AuthLoginResponseDTO
          {
              Uid = (int)u.Uid,
              FullName = u.FullName,
              Email = u.Email,
              Phone = u.Phone,
              Balance = u.Balance,
              BankName = u.BankName,
              BankNumber = u.BankNumber,
              
          };
    }
}