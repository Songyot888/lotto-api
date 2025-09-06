using lotto_api.data;
using Microsoft.AspNetCore.Mvc;

namespace api_lotto.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LottoController : Controller
    {
        private readonly LotteryDbContext _context;
        public LottoController(LotteryDbContext context)
        {
            _context = context;

        }






    }
}