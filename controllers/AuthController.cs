
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


        [HttpGet("user")]
        public async Task<IActionResult> Getall()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
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

    // Assuming you have a DTO for the response, let's call it UserResponseDTO
    // public class UserResponseDTO { public string FullName { get; set; } public string Email { get; set; } public string Phone { get; set; } }
    var responseDto = new RegisterResponseDTO
    {
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone 
    };

    // Return a 201 Created status code with the response DTO
    return CreatedAtAction(nameof(Register), responseDto);
}
    }
}