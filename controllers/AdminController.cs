using api_lotto.DTOs.admin;
using lotto_api.data;
using lotto_api.DTOs.admin;
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Role == dto.Role);

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

            var res = _context.Lotteries.Select(l => new
            {
                l.Lid,
                l.Number,
                l.Price,
                l.Total,
                l.EndDate
            }).ToList();

            await _context.SaveChangesAsync();
            return Ok(new { message = $"{dto.Number} lottery numbers added.", lotto = res });
        }

        [HttpPost("result-lottery")]
        public async Task<IActionResult> ResultLottery([FromBody] AdminOutResult dto)
        {
            const int r1 = 1;
            const int r2 = 2;
            const int r3 = 3;
            const int r4 = 4;
            const int r5 = 5;
            const decimal PayFirst = 6000000m;
            const decimal PaySecond = 2000000m;
            const decimal PayThird = 1000000m;
            const decimal PayLast3 = 4000m;
            const decimal PayLast2 = 2000m;

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Uid == dto.Uid);
            if (user is null) return NotFound(new { message = " ไม่พบผู้ใช้ " });

            if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = "เฉพาะผู้ดูแลระบบเท่านั้นที่สามารถจับสลากหรือประกาศผลได้." });

            var pool = await _context.Lotteries
            .Where(l => l.Status == true)
            .Select(l => new { l.Lid, l.Number })
            .ToListAsync();

            if (pool.Count < 3)
            {
                return BadRequest(new { message = "จำนวนเลขลอตเตอรี่ในระบบที่พร้อมให้สุ่มน้อยเกินไป ต้องมีอย่างน้อย 3 เลขถึงจะออกรางวัลได้" });
            }

            var rand = new Random();
            var picked = new HashSet<int>();

            while (picked.Count < 3)
            {
                picked.Add(rand.Next(pool.Count));
            }

            var idxList = picked.ToList();
            var idx1 = idxList[0];
            var idx2 = idxList[1];
            var idx3 = idxList[2];

            var p1 = pool[idx1];
            var p2 = pool[idx2];
            var p3 = pool[idx3];

            var last3 = p1.Number[^3..];
            var last2 = rand.Next(0, 100).ToString("D2");

            using var tx = await _context.Database.BeginTransactionAsync();


            await _context.Results.ExecuteDeleteAsync();
            await _context.SaveChangesAsync();


            var results = new List<Result>
            {
                new Result { Lid = p1.Lid, PayoutRate = PayFirst  },
                new Result { Lid = p2.Lid, PayoutRate = PaySecond },
                new Result { Lid = p3.Lid, PayoutRate = PayThird  },
            };

            var lidsLast3 = await _context.Lotteries
                .Where(l => l.Status == true && l.Number.EndsWith(last3))
                .Select(l => l.Lid)
                .ToListAsync();

            results.AddRange(lidsLast3.Select(lid => new Result
            {
                Lid = lid,
                PayoutRate = PayLast3
            }));

            var anyLid = pool[rand.Next(pool.Count)].Lid;
            results.Add(new Result
            {
                Lid = anyLid,
                PayoutRate = PayLast2
            });


            _context.Results.AddRange(results);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = "Result completed.",
                prizes = new
                {
                    first = new { result = "รางวัลที่1 ", number = p1.Number, payout = PayFirst },
                    second = new { result = "รางวัลที่2 ", number = p2.Number, payout = PaySecond },
                    third = new { result = "รางวัลที่3 ", number = p3.Number, payout = PayThird },
                    last3 = new { result = "รางวัลที่4", last3, payoutEach = PayLast3 },
                    last2 = new { result = "รางวัลที่5", last2, payoutEach = PayLast2 }
                }
            });
        }
    }
}