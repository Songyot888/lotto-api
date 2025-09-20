using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Lottery")]
[Index("Uid", Name = "fk_lottery_users")]
[MySqlCharSet("utf8mb4")]
[MySqlCollation("utf8mb4_general_ci")]
public partial class Lottery
{
    [Key]
    [Column("lid", TypeName = "int(10) unsigned")]
    public uint Lid { get; set; }

    [Column("uid", TypeName = "int(10) unsigned")]
    public uint Uid { get; set; }

    [Column("number")]
    [StringLength(32)]
    public string? Number { get; set; }

    [Column("price")]
    [Precision(14, 2)]
    public decimal Price { get; set; }

    [Column("total", TypeName = "int(11)")]
    public int? Total { get; set; }

    [Column("date", TypeName = "datetime")]
    public DateTime Date { get; set; }

    [Column("Start_Date")]
    public DateOnly? StartDate { get; set; }

    [Column("End_Date")]
    public DateOnly? EndDate { get; set; }

    [Required]
    [Column("status")]
    public bool? Status { get; set; }

    [InverseProperty("LidNavigation")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [ForeignKey("Uid")]
    [InverseProperty("Lotteries")]
    public virtual User UidNavigation { get; set; } = null!;
}
