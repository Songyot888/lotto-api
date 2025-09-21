using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Result")]
public partial class Result
{
    [Key]
    [Column("rid")]
    public uint Rid { get; set; }

    [Column("payout_rate")]
    [Precision(12, 2)]
    public decimal PayoutRate { get; set; }

    [Column("amount")]
    [StringLength(50)]
    public string Amount { get; set; } = null!;
}
