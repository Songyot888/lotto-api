
using api_lotto.DTOs.auth;
using api_lotto.Helpers;
using api_lotto.Mappers;
using lotto_api.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            if (string.IsNullOrEmpty(dto.FullName) || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new { message = "กรุณากรอกข้อมูลให้ครบ" });
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "อีเมลนี้ถูกใช้ไปแล้ว" });
            }

            var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone);
            if (existingUserByPhone != null)
            {
                return BadRequest(new { message = "เบอร์โทรศัพท์นี้ถูกใช้ไปแล้ว" });
            }

            string hashedPassword = PasswordHelper.HashPassword(dto.Password);
            var user = dto.ToRegister(hashedPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Map the new user to the response DTO
            var RegisterResponse = new RegisterResponseDTO
            {
                FullName = user.FullName,
                Email = user.Email
            };
            // Return the response DTO
            return Ok(RegisterResponse);
        }
    }
}