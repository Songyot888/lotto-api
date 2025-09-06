namespace api_lotto.DTOs.admin
{
    public class AdminRenDTO
    {
<<<<<<< HEAD

=======
        public string Role { get; set; } = string.Empty;
>>>>>>> 330e7f2964e65def199b80e60beae121eb170297
        public decimal Number { get; set; }
    }
    
    public class _NewLotteryResponseDTO
    {
        public int Lid { get; set; }
        public string Number { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int total { get; set; } = 1;
        public DateOnly? End_Date { get; set; } = null;

    }
}