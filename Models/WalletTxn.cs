using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("WalletTxn")]
[Index("Uid", Name = "fk_wallettxn_users")]
public partial class WalletTxn
{
    [Key]
    [Column("wid")]
    public ulong Wid { get; set; }

    [Column("uid")]
    public uint Uid { get; set; }

    [Column("top_up")]
    [Precision(14, 2)]
    public decimal? TopUp { get; set; }

    [Column("withdraw")]
    [Precision(14, 2)]
    public decimal? Withdraw { get; set; }

    [Column("status")]
    public bool Status { get; set; }

    [Column("date", TypeName = "datetime")]
    public DateTime Date { get; set; }

    [ForeignKey("Uid")]
    [InverseProperty("WalletTxns")]
    public virtual User UidNavigation { get; set; } = null!;
}
