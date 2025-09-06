using lotto_api.data;
using Microsoft.AspNetCore.Mvc;

namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly LotteryDbContext _context;

        // Constructor: รับ DbContext จาก DI
        public WalletController(LotteryDbContext context)
        {
            _context = context;
        }

        

        // -------------------------------
        // 1. ดูยอดเงินใน Wallet
        // -------------------------------
        [HttpGet("{memberId}")]
        public IActionResult GetWallet(uint memberId)
        {
            // หา user จากฐานข้อมูล
            var user = _context.Users.FirstOrDefault(u => u.Uid == memberId);
            if (user == null)
                return NotFound(new { message = "ไม่พบสมาชิก" });

            // คำนวณยอดเงินจาก WalletTxn
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
    }
}
