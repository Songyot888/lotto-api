using lotto_api.data;
using Microsoft.AspNetCore.Mvc;
using lotto_api.Models;
using Microsoft.EntityFrameworkCore;


namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        public UserController(LotteryDbContext context)
        {
            _context = context;
        }


        //เลือกดูและค้นหารายการลอตเตอรี่ที่มีในระบบที่ยังไม่ขายออกได้
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


        //ซื้อหวย
        [HttpPost("buy")] // เรียกแบบ: POST /api/LottoUser/buy?memberId=34&lotteryId=3
        public IActionResult BuyLottery([FromQuery] uint memberId, [FromQuery] uint lotteryId)
        {
            using var transaction = _context.Database.BeginTransaction(); // เปิด Transaction เพื่อให้ทุกขั้นตอนสำเร็จ/ล้มเหลวพร้อมกัน
            try
            {
                // 1) หา User ผู้ซื้อจากตาราง Users ตาม uid ที่ส่งมา
                var user = _context.Users.FirstOrDefault(u => u.Uid == memberId);
                if (user == null)
                    return NotFound(new { message = "ไม่พบสมาชิก" }); // ถ้าไม่มีผู้ใช้ → 404

                // 2) หา Lottery ใบที่ต้องการซื้อ และต้องยังขายได้ (Status = true)
                var lottery = _context.Lotteries.FirstOrDefault(l => l.Lid == lotteryId && l.Status == true);
                if (lottery == null)
                    return BadRequest(new { message = "ลอตเตอรี่ถูกขายแล้วหรือไม่มีอยู่" }); // ไม่พบหรือถูกขายแล้ว → 400

                // 3) ตรวจสอบยอดเงินใน Wallet ของผู้ใช้ว่าพอจ่ายราคาใบนี้ไหม
                if (user.Balance < lottery.Price)
                    return BadRequest(new { message = "ยอดเงินใน Wallet ไม่พอ" }); // เงินไม่พอ → 400

                // 4) ตัดเงินจาก Wallet: ยอดคงเหลือ = ยอดเดิม - ราคา
                user.Balance -= lottery.Price;

                // 5) อัปเดตสถานะลอตเตอรี่ → ขายแล้ว และบันทึกเจ้าของ (uid) เป็นผู้ซื้อ
                lottery.Status = false;     // false = ขายแล้ว
                lottery.Uid = memberId;     // กำหนดว่า user นี้คือเจ้าของ

                // 6) บันทึกรายการกระเป๋าเงิน (WalletTxn) เป็นการถอนเงินออกเพื่อซื้อหวย
                var txn = new WalletTxn
                {
                    Uid = memberId,         // ใครทำธุรกรรม
                    Withdraw = lottery.Price, // เงินที่ถูกหักออก
                    TopUp = null,             // ไม่มีการเติมเงินกรณีนี้
                    Status = true,            // ทำธุรกรรมสำเร็จ
                    Date = DateTime.Now       // เวลาที่เกิดรายการ
                };
                _context.WalletTxns.Add(txn); // คิวรอ insert


                // 7) สร้าง Order เพื่อเก็บประวัติว่าผู้ใช้คนนี้ซื้อหวยใบไหน เมื่อไหร่
                var order = new Order
                {
                    Uid = memberId,         // ผู้ซื้อ
                    Lid = lotteryId,        // หวยที่ซื้อ
                    Date = DateTime.Now     // เวลา
                };
                _context.Orders.Add(order); // คิวรอ insert

                // 8) บันทึกการเปลี่ยนแปลงทั้งหมดลงฐานข้อมูล (Users, Lotteries, WalletTxns, Orders)
                _context.SaveChanges();

                // 9) ทุกอย่างผ่าน → ยืนยัน Transaction
                transaction.Commit();

                // 10) ตอบกลับผลลัพธ์ให้ Client เห็นเลขที่ซื้อ ราคา และยอดคงเหลือ
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
                transaction.Rollback(); // เกิดข้อผิดพลาด → ยกเลิกทุกการเปลี่ยนแปลง
                return StatusCode(500, new { message = "เกิดข้อผิดพลาด", error = ex.Message }); // ตอบ 500 พร้อมข้อความ error
            }
        }


        [HttpGet("checkwallet")]
        public IActionResult GetWallet([FromRoute] uint memberId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Uid == memberId);
            if (user == null)
                return NotFound(new { message = "ไม่พบสมาชิก" });

            var balance = _context.Users
                .Where(w => w.Uid == memberId)
                .Select(u => new
                {
                    u.Uid,
                    u.FullName,
                    u.Balance
                })
                .FirstOrDefault();

            return Ok(balance);
        }


        [HttpGet("check")]
        public IActionResult CheckResult([FromQuery] uint memberId, [FromQuery] ulong orderId)
        {
            // 1) ไปหาข้อมูลการสั่งซื้อ (Order) ของ user ที่ส่งมา
            var order = _context.Orders
                .Include(o => o.LidNavigation)        
                .ThenInclude(l => l.Results)       
                .FirstOrDefault(o => o.Oid == orderId && o.Uid == memberId);

            // ถ้าไม่เจอเลย แสดงว่า orderId นี้ไม่ใช่ของ user หรือไม่มีอยู่จริง
            if (order == null)
            {
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อ" });
            }

            // lottery ที่ user ซื้อมา
            var lottery = order.LidNavigation;

            // 2) ตรวจว่ามีการประกาศผลรางวัลแล้วหรือยัง
            var result = lottery.Results.FirstOrDefault();
            if (result == null)
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

            // 3) ถ้ามีผลแล้ว คำนวณรางวัลจากราคาหวย * payout rate
            decimal prize = lottery.Price * result.PayoutRate;

            // 4) ตอบกลับ โดยยังไม่ได้บวกเงินเข้ากระเป๋า
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
        public IActionResult ClaimPrize([FromQuery] uint memberId, [FromQuery] ulong orderId)
        {
            // 1) หา Order ของ user ที่ต้องการขึ้นเงิน
            var order = _context.Orders
                .Include(o => o.LidNavigation)
                .ThenInclude(l => l.Results)
                .FirstOrDefault(o => o.Oid == orderId && o.Uid == memberId);

            if (order == null)
            {
                return NotFound(new { message = "ไม่พบข้อมูลการซื้อ" });
            }

            var lottery = order.LidNavigation;
            var result = lottery.Results.FirstOrDefault();

            // ถ้ายังไม่มีผลรางวัล
            if (result == null)
            {
                return BadRequest(new { message = "ยังไม่มีการออกรางวัล" });
            }

            // 2) คำนวณเงินรางวัล
            decimal prize = lottery.Price * result.PayoutRate;
            if (prize <= 0)
            {
                return BadRequest(new { message = "ไม่ถูกรางวัล" });
            }

            // 3) กันการขึ้นเงินซ้ำ → เช็กจาก WalletTxn ว่าเคยโอนเงินรางวัลไปแล้วหรือยัง
            bool alreadyClaimed = _context.WalletTxns.Any(t =>
                t.Uid == memberId &&
                t.TopUp == prize &&
                t.Date.Date == DateTime.Now.Date
            );

            if (alreadyClaimed)
            {
                return BadRequest(new { message = "ลอตเตอรี่ใบนี้ขึ้นเงินไปแล้ว", lotteryId = lottery.Lid });
            }

            // 4) ถ้าไม่เคยขึ้น → บวกเงินเข้ากระเป๋าของ user
            var user = _context.Users.First(u => u.Uid == memberId);
            user.Balance += prize;

            // 5) บันทึกธุรกรรมการเติมเงินรางวัล
            var txn = new WalletTxn
            {
                Uid = memberId,
                TopUp = prize,
                Withdraw = null,
                Status = true,
                Date = DateTime.Now
            };
            _context.WalletTxns.Add(txn);

            // 6) เซฟข้อมูลลงฐานข้อมูล
            _context.SaveChanges();

            // 7) ตอบกลับผลลัพธ์
            return Ok(new
            {
                message = "ขึ้นเงินสำเร็จ",
                lotteryId = lottery.Lid,
                amount = prize,
                wallet = user.Balance
            });
        }

    }
}
