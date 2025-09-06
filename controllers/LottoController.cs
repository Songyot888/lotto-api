namespace lotto_api.controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LottoController : ControllerBase
    {
        private readonly LotteryDbContext _context;
        public LottoController(LotteryDbContext context)
        {
            _context = context;
        }
        
        
        

    }
}