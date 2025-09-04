using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Order")]
[Index("Lid", Name = "fk_order_lottery")]
[Index("Uid", Name = "fk_order_users")]
public partial class Order
{
    [Key]
    [Column("oid")]
    public ulong Oid { get; set; }

    [Column("uid")]
    public uint Uid { get; set; }

    [Column("lid")]
    public uint Lid { get; set; }

    [Column("date", TypeName = "datetime")]
    public DateTime Date { get; set; }

    [ForeignKey("Lid")]
    [InverseProperty("Orders")]
    public virtual Lottery LidNavigation { get; set; } = null!;

    [ForeignKey("Uid")]
    [InverseProperty("Orders")]
    public virtual User UidNavigation { get; set; } = null!;
}
