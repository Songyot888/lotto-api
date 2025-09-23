
using System.Globalization;
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

        [HttpGet("allLotto")]
        public IActionResult allLotto()
        {
            var query = _context.Lotteries
            .Select(l => new
            {
                l.Lid,
                l.Number,
                l.Price,
                l.Total,
                l.Date,
                l.StartDate,
                l.EndDate,
                l.Status
            }).ToList();
            return Ok(query);
        }



        [HttpPost("buy")]
        public IActionResult BuyLottery([FromBody] buyDTO dto)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Uid == dto.memberId);
                if (user == null)
                    return NotFound(new { message = "ไม่พบสมาชิก" });

                var lottery = _context.Lotteries
                    .FirstOrDefault(l => l.Lid == dto.lotteryId && l.Status == true);
                if (lottery == null)
                    return BadRequest(new { message = "ลอตเตอรี่ถูกขายแล้วหรือไม่มีอยู่" });

                if (user.Balance < lottery.Price)
                    return BadRequest(new { message = "ยอดเงินใน Wallet ไม่พอ" });

                // ปรับยอด/สถานะ
                user.Balance -= lottery.Price;
                lottery.Status = false;

                var order = new Order
                {
                    Uid = (uint)dto.memberId,
                    Lid = (uint)dto.lotteryId,
                    Date = DateTime.UtcNow   // แนะนำใช้ UTC
                };
                _context.Orders.Add(order);

                _context.SaveChanges(); // EF จะทำใน transaction ให้อยู่แล้ว

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
            var orderEntity = _context.Orders
                .Include(o => o.LidNavigation)
                .FirstOrDefault(o => o.Uid == dto.memberId && o.Lid == dto.lid);

            if (orderEntity == null)
            {
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อของ user กับหวยใบนี้" });
            }

            var lottery = orderEntity.LidNavigation;

            var results = _context.Results.ToList();
            if (results.Count == 0)
            {
                return Ok(new
                {
                    message = "ยังไม่มีการออกรางวัล",
                    isWin = false,
                    prize = 0
                });
            }

            var matched = results.FirstOrDefault(r => lottery.Number != null &&
                                                      lottery.Number.EndsWith(r.Amount.ToString()));

            if (matched != null)
            {
                // กรณีถูกรางวัล
                decimal prize = matched.PayoutRate;
                return Ok(new
                {
                    isWin = true,
                    message = "ถูกรางวัล",
                    lotteryId = lottery.Lid,
                    number = lottery.Number,
                    prize = prize
                });
            }
            else
            {

                orderEntity.Status = true;
                _context.SaveChanges();

                return Ok(new
                {
                    isWin = false,
                    message = "ไม่ถูกรางวัล",
                    prize = 0
                });
            }
        }


        [HttpPost("claim")]
        public IActionResult ClaimPrize([FromBody] claimDTO dto)
        {
            var order = _context.Orders
                .FirstOrDefault(o => o.Uid == (uint)dto.memberId
                                     && o.Oid == (uint)dto.orderId);

            if (order == null)
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อ" });

            // เช็คว่าขึ้นเงินไปแล้วหรือยัง (Status == false = ขึ้นเงินแล้ว)
            if (order.Status == false)
                return Conflict(new { message = "ขึ้นเงินไปแล้ว" });

            var lottery = _context.Lotteries.FirstOrDefault(l => l.Lid == order.Lid);
            if (lottery == null)
                return NotFound(new { message = "ไม่พบลอตเตอรี่" });

            var results = _context.Results
                .Select(r => new { r.Amount, r.PayoutRate })
                .ToList();

            if (results.Count == 0)
                return BadRequest(new { message = "ยังไม่มีการออกรางวัล" });

            var matched = results.FirstOrDefault(r => lottery.Number != null &&
                                                      lottery.Number.EndsWith(r.Amount.ToString()));
            var prize = matched?.PayoutRate ?? 0m;

            if (prize <= 0)
            {
                // ไม่ถูกรางวัล - เปลี่ยนสถานะเป็น true (ไม่ถูก)
                order.Status = true;
                _context.SaveChanges();

                return BadRequest(new
                {
                    message = "ไม่ถูกรางวัล",
                    lotteryId = lottery.Lid,
                    number = lottery.Number,
                    amount = 0,
                    status = "ไม่ถูกรางวัล"
                });
            }

            // ถูกรางวัล - จ่ายเงินและปิดออเดอร์
            var user = _context.Users.First(u => u.Uid == (uint)dto.memberId);
            user.Balance += prize;
            order.Status = false; // 0 = ขึ้นเงินแล้ว

            _context.SaveChanges();

            return Ok(new
            {
                message = "ขึ้นเงินสำเร็จ",
                lotteryId = lottery.Lid,
                number = lottery.Number,
                amount = prize,
                wallet = user.Balance,
                orderStatus = order.Status
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

        [HttpPost("Txnwallet")]
        public IActionResult Txnwallet([FromBody] TxnwalletDTO dto)
        {
            var result = _context.WalletTxns
            .Where(u => u.Uid == dto.memberId)
            .Select(s => new
            {
                wid = s.Wid,
                uid = dto.memberId,
                topUp = s.TopUp,
                withdraw = s.Withdraw,
                status = s.Status,
                date = s.Date
            }).ToList();

            return Ok(result);
        }

        [HttpPost("Lottol3")]
        public async Task<IActionResult> Lottol3([FromBody] allLottoDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "ข้อมูลว่าง" });

            var term = new string((dto.number_lottory.ToString() ?? "")
                                  .Where(char.IsDigit).ToArray());

            if (term.Length != 3)
                return BadRequest(new { message = "ใส่เลข 3 หลัก" });

            var result = await _context.Lotteries
                .AsNoTracking()
                .Where(l => l.Status == true
                            && l.Number != null
                            && l.Number.EndsWith(term))
                .OrderBy(l => l.Number)
                .Select(l => new { lid = l.Lid, number = l.Number, price = l.Price })
                .ToListAsync();

            return Ok(result);
        }


        [HttpPost("Lottof3")]
        public async Task<IActionResult> Lottof3([FromBody] allLottoDTO dto)
        {

            if (dto == null) return BadRequest(new { message = "ข้อมูลว่าง" });

            var term = new string((dto.number_lottory.ToString() ?? "")
                                  .Where(char.IsDigit).ToArray());

            if (term.Length != 3)
                return BadRequest(new { message = "ใส่เลข 3 หลัก" });

            var result = await _context.Lotteries
                .AsNoTracking()
                .Where(l => l.Status == true
                            && l.Number != null
                            && l.Number.StartsWith(term))
                .OrderBy(l => l.Number)
                .Select(l => new { lid = l.Lid, number = l.Number, price = l.Price })
                .ToListAsync();

            return Ok(result);
        }
        [HttpPost("Lottol2")]
        public async Task<IActionResult> Lottol2([FromBody] allLottoDTO dto)
        {

            if (dto == null) return BadRequest(new { message = "ข้อมูลว่าง" });

            var term = new string((dto.number_lottory.ToString() ?? "")
                                  .Where(char.IsDigit).ToArray());

            if (term.Length != 2)
                return BadRequest(new { message = "ใส่เลข 2 หลัก" });

            var result = await _context.Lotteries
                .AsNoTracking()
                .Where(l => l.Status == true
                            && l.Number != null
                            && l.Number.EndsWith(term))
                .OrderBy(l => l.Number)
                .Select(l => new { lid = l.Lid, number = l.Number, price = l.Price })
                .ToListAsync();

            return Ok(result);
        }



        [HttpPost("TxnLotto")]
        public async Task<IActionResult> TxnLottoAsync([FromBody] TxnLottoDTO dto)
        {
            if (dto == null) return BadRequest(new { message = "ข้อมูลว่าง" });

            var raw = await _context.Orders
                .Where(o => o.Uid == (uint)dto.memberId)
                .OrderByDescending(o => o.Date)
                .Select(o => new
                {
                    o.Oid,
                    o.Lid,
                    Number = o.LidNavigation.Number,
                    o.Date,
                    o.Status
                })
                .ToListAsync();

            var th = new CultureInfo("th-TH");
            var thaiTz = GetThaiTimeZone();

            var result = raw.Select(x =>
            {
                var utc = x.Date.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(x.Date, DateTimeKind.Utc)
                            : x.Date.ToUniversalTime();

                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, thaiTz);

                return new
                {
                    oid = x.Oid,
                    lotteryId = x.Lid,
                    number = x.Number,
                    status = x.Status,
                    dateTh = local.ToString("d MMM yyyy", th),
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("MyLotto/{uid}")]
        public async Task<IActionResult> MyLotto([FromRoute] int uid)
        {
            var myLotto = await (
                from o in _context.Orders
                join l in _context.Lotteries on o.Lid equals l.Lid
                where o.Uid == uid
                orderby o.Oid descending
                select new
                {
                    oid = o.Oid,
                    lotteryId = l.Lid,
                    number = l.Number
                }
            ).ToListAsync();

            if (myLotto == null || !myLotto.Any())
                return NotFound(new { message = "ไม่พบลอตเตอรี่ของผู้ใช้นี้" });

            return Ok(myLotto);
        }
        private static TimeZoneInfo GetThaiTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }          // Linux/macOS
            catch { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } // Windows
        }

    }
}
