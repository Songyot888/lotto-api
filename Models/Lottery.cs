using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Table("Lottery")]
[Index("Uid", Name = "fk_lottery_users")]
public partial class Lottery
{
    [Key]
    [Column("lid")]
    public uint Lid { get; set; }

    [Column("uid")]
    public uint Uid { get; set; }

    [Column("number")]
    [StringLength(32)]
    public string? Number { get; set; }

    [Column("price")]
    [Precision(14, 2)]
    public decimal Price { get; set; }

    [Column("total")]
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

    [InverseProperty("LidNavigation")]
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    [ForeignKey("Uid")]
    [InverseProperty("Lotteries")]
    public virtual User UidNavigation { get; set; } = null!;
}
