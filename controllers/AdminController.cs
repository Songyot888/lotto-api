using api_lotto.DTOs.admin;
using lotto_api.Data;
using lotto_api.DTOs.admin;
using lotto_api.Mappers;
using lotto_api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        public AdminController(ApplicationDBContext context)
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Role == dto.Role);

            if (user == null)
            {
                return NotFound(new { message = "Admin user not found." });
            }

            if (user.Role != "admin")
            {
                return NotFound(new { message = "คุณไม่ใช่ผู้ดูแลระบบ." });
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
            var res = _context.Lotteries.Select(l => new
            {
                l.Lid,
                l.Number,
                l.Price,
                l.Total,
                l.EndDate
            }).ToList();


            return Ok(new { message = $"{dto.Number} lottery numbers added.", lotto = res });
        }

        [HttpPost("result-lottery")]
        public async Task<IActionResult> ResultLottery([FromBody] AdminOutResult dto)
        {
            const decimal PayFirst = 6000000m;
            const decimal PaySecond = 2000000m;
            const decimal PayThird = 1000000m;
            const decimal PayLast3 = 4000m;
            const decimal PayLast2 = 2000m;

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Uid == dto.Uid);
            if (user is null) return NotFound(new { message = " ไม่พบผู้ใช้ " });
            if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = "เฉพาะผู้ดูแลระบบเท่านั้นที่สามารถจับสลากหรือประกาศผลได้." });

            if (await _context.Results.AnyAsync())
                return BadRequest(new { message = "มีการออกรางวัลแล้ว กรุณารีเซ็ตระบบก่อนทำการสุ่มใหม่" });

            var pool = await _context.Lotteries
                .Where(l => l.Status == true && l.Number != null)
                .Select(l => l.Number!.Trim())
                .ToListAsync();

            if (pool.Count < 3)
                return BadRequest(new { message = "จำนวนเลขลอตเตอรี่ในระบบที่พร้อมให้สุ่มน้อยเกินไป ต้องมีอย่างน้อย 3 เลขถึงจะออกรางวัลได้" });

            var rand = new Random();


            var picked = new HashSet<int>();
            while (picked.Count < 3) picked.Add(rand.Next(pool.Count));
            var idx = picked.ToList();
            var n1 = pool[idx[0]];
            var n2 = pool[idx[1]];
            var n3 = pool[idx[2]];

            var last3 = n1[^3..];
            var last2 = pool[rand.Next(pool.Count)][^2..];


            var results = new List<Result>
    {
        new Result { PayoutRate = PayFirst,  Amount = n1 },
        new Result { PayoutRate = PaySecond, Amount = n2 },
        new Result { PayoutRate = PayThird,  Amount = n3 },
        new Result { PayoutRate = PayLast3,  Amount = last3 },
        new Result { PayoutRate = PayLast2,  Amount = last2 },
    };

            _context.Results.AddRange(results);
            await _context.SaveChangesAsync();


            return Ok(new
            {
                message = "Result completed.",
                prizes = new
                {
                    first = new { result = "รางวัลที่1 ", number = n1, payout = PayFirst },
                    second = new { result = "รางวัลที่2 ", number = n2, payout = PaySecond },
                    third = new { result = "รางวัลที่3 ", number = n3, payout = PayThird },
                    last3 = new { result = "รางวัลเลขท้าย 3 ตัว", last3, payoutEach = PayLast3 },
                    last2 = new { result = "รางวัลเลขท้าย 2 ตัว", last2, payoutEach = PayLast2 }
                }
            });
        }


        [HttpPost("clear")]
        public async Task<IActionResult> ClearData([FromBody] Admin_ResetDTO dTO)
        {
            _context.Orders.RemoveRange(_context.Orders);
            _context.Results.RemoveRange(_context.Results);
            _context.Lotteries.RemoveRange(_context.Lotteries);
            _context.WalletTxns.RemoveRange(_context.WalletTxns);
            var usersToDelete = await _context.Users
                .Where(u => u.Role != "ADMIN")
                .ToListAsync();
            _context.Users.RemoveRange(usersToDelete);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "ล้างข้อมูลสำเร็จ"
            });
        }
    }
}