// using api_lotto.DTOs.admin;
// using api_lotto.DTOs.auth;
// using lotto_api.Models;
// // using lotto_api.Models;

// namespace lotto_api.Mappers
// {
    
//     public static class AdminRenMapper
//     {


//         public static _NewLotteryResponseDTO To_NewLottoResponse(this Lottery l) =>
//           new _NewLotteryResponseDTO
//           {
//               Lid = (int)l.Lid,
//               Number = l.Number ?? string.Empty,
//               Price = l.Price,
//               total = l.Total ?? 1,
//               End_Date = l.EndDate,
//           };
//     }
// }