using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Result")]
[Index("Lid", Name = "fk_result_lottery")]
public partial class Result
{
    [Key]
    [Column("rid")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public uint Rid { get; set; }

    [Column("lid")]
    public uint Lid { get; set; }

    [Column("payout_rate")]
    [Precision(12, 2)]
    public decimal PayoutRate { get; set; }

    [ForeignKey("Lid")]
    [InverseProperty("Results")]
    public virtual Lottery LidNavigation { get; set; } = null!;
}
