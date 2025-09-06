using api_lotto.DTOs.admin;
using lotto_api.data;
using lotto_api.Mappers;
using lotto_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        public AdminController(LotteryDbContext context)
        {
            _context = context;
        }

        [HttpPost("add-lottery")]
        public async Task<IActionResult> AddLotteryNumbers([FromBody] AdminRenDTO dto)
        {
            var random = new Random();
            var numbers = new HashSet<string>();

            var existing = await _context.Lotteries
                .Select(x => x.Number!)
                .Where(x => x != null)
                .ToHashSetAsync();
            while (numbers.Count < dto.Number)
            {
                var number = random.Next(0, 1_000_000).ToString("D6");
                if (existing.Contains(number)) continue;
                numbers.Add(number);
                existing.Add(number);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Role == dto.Role );

            if (user == null)
            {
                return NotFound(new { message = "Admin user not found." });
            }

            var lotto = new Lottery();
            foreach (var num in numbers)
            {
                lotto = new Lottery
                {
                    Uid = user.Uid,
                    Number = num,
                    Price = 100.00m,
                    Total = 1,
                    Date = DateTime.Now,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                    Status = true
                };

                _context.Lotteries.Add(lotto);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"{dto.Number} lottery numbers added.", lotto = lotto.To_NewLottoResponse() });
        }
    }
}