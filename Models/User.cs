using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace lotto_api.Models;

[Index("Email", Name = "email", IsUnique = true)]
[Index("Phone", Name = "phone", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("uid")]
    public uint Uid { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column("full_name")]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Column("role")]
    [StringLength(20)]
    public string? Role { get; set; }

    [Column("bank_name")]
    [StringLength(100)]
    public string? BankName { get; set; }

    [Column("bank_number")]
    [StringLength(30)]
    public string? BankNumber { get; set; }

    [Column("balance")]
    [Precision(14, 2)]
    public decimal Balance { get; set; }

    [Column("date", TypeName = "datetime")]
    public DateTime Date { get; set; }

    [InverseProperty("UidNavigation")]
    public virtual ICollection<Lottery> Lotteries { get; set; } = new List<Lottery>();

    [InverseProperty("UidNavigation")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("UidNavigation")]
    public virtual ICollection<WalletTxn> WalletTxns { get; set; } = new List<WalletTxn>();
}
