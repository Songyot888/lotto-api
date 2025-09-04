
using api_lotto.DTOs.auth;
using api_lotto.Helpers;
using api_lotto.Mappers;
using lotto_api.data;
using Microsoft.AspNetCore.Mvc;

namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly LotteryDbContext _context;
        public AuthController(LotteryDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult>  Register([FromBody] RegisterDTO dto)
        {
            if (string.IsNullOrEmpty(dto.FullName) || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new { message = "กรุณากรอกข้อมูลให้ครบ" });
            }
            else
            {
                string hashedPassword = PasswordHelper.HashPassword(dto.Password);
                var user = dto.ToRegister(hashedPassword);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            
            return Ok();
        }
    }
}