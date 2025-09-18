using lotto_api.data;
using Microsoft.AspNetCore.Mvc;

namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        public WalletController(LotteryDbContext context)
        {
            _context = context;
        }

        
        [HttpGet("{memberId}")]
        public IActionResult GetWallet(uint memberId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Uid == memberId);
            if (user == null)
                return NotFound(new { message = "ไม่พบสมาชิก" });

            var balance = _context.WalletTxns
                .Where(w => w.Uid == memberId && w.Status == true)
                .Sum(w => (w.TopUp ?? 0) - (w.Withdraw ?? 0));

            // ส่งออกเป็น JSON
            return Ok(new
            {
                user.Uid,
                user.FullName,
                wallet = balance
            });
        }


        [HttpGet("unsold")]
        public IActionResult GetUnsoldLotteries()
        {
            // ดึงเฉพาะที่ยังไม่ขายออก (Status = 1)
            var query = _context.Lotteries
                .Where(l => l.Status == true);

            var result = query.Select(l => new
            {
                l.Lid,
                l.Number,
                l.Price,
                l.Total,
                l.Date,
                l.StartDate,
                l.EndDate
            }).ToList();

            return Ok(result);
        }


    }
}
