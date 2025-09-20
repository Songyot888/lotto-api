
using lotto_api.Data;
using lotto_api.DTOs.lottory;
using lotto_api.Models;
using Microsoft.AspNetCore.Mvc;
// using lotto_api.Models;
using Microsoft.EntityFrameworkCore;
// using lotto_api.Data;


namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        public UserController(ApplicationDBContext context)
        {
            _context = context;
        }



        [HttpGet("result")]
        public async Task<IActionResult> GetResultSummary()
        {

            var result = await _context.Results.Select(s => new
            {
                s.PayoutRate,
                s.Amount
            }).ToListAsync();
            return Ok(new
            {
                message = "รางวัลที่ออก",
                result
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


        [HttpPost("buy")]
        public IActionResult BuyLottery([FromBody] buyDTO dto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Uid == dto.memberId);
                if (user == null)
                    return NotFound(new { message = "ไม่พบสมาชิก" });

                var lottery = _context.Lotteries.FirstOrDefault(l => l.Lid == dto.lotteryId && l.Status == true);
                if (lottery == null)
                    return BadRequest(new { message = "ลอตเตอรี่ถูกขายแล้วหรือไม่มีอยู่" });

                if (user.Balance < lottery.Price)
                    return BadRequest(new { message = "ยอดเงินใน Wallet ไม่พอ" });

                // ตัดเงิน
                user.Balance -= lottery.Price;

                // อัปเดตสถานะลอตเตอรี่ (ขายแล้ว) ✅ ไม่เปลี่ยน Uid
                lottery.Status = false;


                // บันทึก Order → ใครซื้อใบนี้
                var order = new Order
                {
                    Uid = (uint)dto.memberId,
                    Lid = (uint)dto.lotteryId,
                    Date = DateTime.Now
                };
                _context.Orders.Add(order);

                _context.SaveChanges();
                transaction.Commit();

                return Ok(new
                {
                    message = "ซื้อสำเร็จ",
                    orderId = order.Oid,
                    lotteryId = lottery.Lid,
                    number = lottery.Number,
                    price = lottery.Price,
                    wallet = user.Balance
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "เกิดข้อผิดพลาด", error = ex.Message });
            }
        }



        [HttpPost("checkwallet")]
        public IActionResult GetWallet([FromBody] checkwalletDTO dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Uid == dto.memberId);
            if (user == null)
                return NotFound(new { message = "ไม่พบสมาชิก" });

            var balance = _context.Users
                .Where(w => w.Uid == dto.memberId)
                .Select(u => new
                {
                    u.Uid,
                    u.FullName,
                    u.Balance
                })
                .FirstOrDefault();

            return Ok(balance);
        }


        [HttpPost("check")]
        public IActionResult CheckResult([FromBody] checkLottortDTO dto)
        {
            var order = _context.Orders
                .Where(o => o.Uid == dto.memberId && o.Lid == dto.lid)
                .Select(s => new
                {
                    s.Oid,
                    Lottery = s.LidNavigation
                })
                .FirstOrDefault();

            if (order == null)
            {
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อของ user กับหวยใบนี้" });
            }

            var lottery = order.Lottery;

            var results = _context.Results.ToList();
            if (results.Count == 0)
            {
                return Ok(new
                {
                    message = "ยังไม่มีการออกรางวัล",
                    lotteryId = lottery.Lid,
                    number = lottery.Number,
                    prize = 0,
                    status = "ยังไม่ขึ้นเงิน"
                });
            }

            var matched = results.FirstOrDefault(r => lottery.Number!.EndsWith(r.Amount.ToString()));
            decimal prize = matched?.PayoutRate ?? 0;

            return Ok(new
            {
                message = prize > 0 ? "ถูกรางวัล" : "ไม่ถูกรางวัล",
                lotteryId = lottery.Lid,
                number = lottery.Number,
                prize,
                status = "ยังไม่ขึ้นเงิน"
            });
        }



        [HttpPost("claim")]
        public IActionResult ClaimPrize([FromBody] claimDTO dto)
        {
            // 1) หา Order ของ user สำหรับใบที่ระบุ (cast ให้ชนิดตรงกับ model)
            var order = _context.Orders
                .Where(o => o.Uid == (uint)dto.memberId && o.Oid == (uint)dto.orderId)
                .Select(s => new
                {
                    s.Lid,
                    Lottery = s.LidNavigation
                })
                .FirstOrDefault();

            if (order == null)
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อ" });

            var lottery = order.Lottery;

            // 2) ต้องมีการออกรางวัลแล้ว
            var results = _context.Results
                .Select(r => new { r.Amount, r.PayoutRate })
                .ToList();

            if (results.Count == 0)
                return BadRequest(new { message = "ยังไม่มีการออกรางวัล" });

            var matched = results.FirstOrDefault(r => lottery.Number != null && lottery.Number.EndsWith(r.Amount.ToString()));
            var prize = matched?.PayoutRate ?? 0m;

            if (prize <= 0)
            {
                return BadRequest(new
                {
                    message = "ไม่ถูกรางวัล",
                    lotteryId = lottery.Lid,
                    number = lottery.Number,
                    amount = 0,
                    status = "ไม่ถูกรางวัลจริงๆ"
                });
            }

            var user = _context.Users.First(u => u.Uid == (uint)dto.memberId);
            user.Balance += prize;

            _context.SaveChanges();

            return Ok(new
            {
                message = "ขึ้นเงินสำเร็จ",
                lotteryId = lottery.Lid,
                number = lottery.Number,
                amount = prize,
                wallet = user.Balance
            });
        }


        [HttpPost("topup")]
        public IActionResult TopUp([FromBody] TopupDTO dto)
        {
            try
            {
                // 1) หา user
                var user = _context.Users.FirstOrDefault(u => u.Uid == (uint)dto.memberId);
                if (user == null)
                    return NotFound(new { message = "ไม่พบผู้ใช้" });

                if (dto.money <= 0)
                    return BadRequest(new { message = "จำนวนเงินต้องมากกว่า 0" });

                user.Balance += dto.money;

                var txn = new WalletTxn
                {
                    Uid = (uint)dto.memberId,
                    TopUp = dto.money,
                    Withdraw = 0,
                    Status = true,
                    Date = DateTime.Now
                };
                _context.WalletTxns.Add(txn);

                // 4) เซฟลง DB
                _context.SaveChanges();

                // 5) ตอบกลับ
                return Ok(new
                {
                    message = "เติมเงินสำเร็จ",
                    memberId = user.Uid,
                    amount = dto.money,
                    wallet = user.Balance
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาด", error = ex.Message });
            }
        }

        [HttpPost("withdraw")]
        public IActionResult withdraw([FromBody] withdrawDTO dto)
        {
            try
            {
                // 1) หา user
                var user = _context.Users.FirstOrDefault(u => u.Uid == (uint)dto.memberId);
                if (user == null)
                    return NotFound(new { message = "ไม่พบผู้ใช้" });

                if (dto.money <= 0)
                    return BadRequest(new { message = "จำนวนเงินต้องมากกว่า 0" });



                if (dto.money > user.Balance)
                {
                    return BadRequest(new { message = "ยอดเงินไม่เพียงพอ" });
                }
                else
                {
                    user.Balance -= dto.money;
                }

                var txn = new WalletTxn
                {
                    Uid = (uint)dto.memberId,
                    TopUp = 0,
                    Withdraw = dto.money,
                    Status = true,
                    Date = DateTime.Now
                };
                _context.WalletTxns.Add(txn);

                // 4) เซฟลง DB
                _context.SaveChanges();

                // 5) ตอบกลับ
                return Ok(new
                {
                    message = "ถอนเงินสำเร็จ",
                    memberId = user.Uid,
                    amount = dto.money,
                    wallet = user.Balance
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "เกิดข้อผิดพลาด", error = ex.Message });
            }
        }

    }
}
