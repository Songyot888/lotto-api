using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Result")]
[MySqlCharSet("utf8mb4")]
[MySqlCollation("utf8mb4_general_ci")]
public partial class Result
{
    [Key]
    [Column("rid", TypeName = "int(10) unsigned")]
    public uint Rid { get; set; }

    [Column("payout_rate")]
    [Precision(12, 2)]
    public decimal PayoutRate { get; set; }

    [Column("amount")]
    [StringLength(50)]
    public string Amount { get; set; } = null!;
}
