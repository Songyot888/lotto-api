using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using lotto_api.Models;

namespace lotto_api.Data;

public partial class ApplicationDBContext : DbContext
{
    public ApplicationDBContext()
    {
    }

    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Lottery> Lotteries { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WalletTxn> WalletTxns { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=202.28.34.203;database=mb68_66011212090;user=mb68_66011212090;password=X)iPhkST&bqz", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.4.0-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Lottery>(entity =>
        {
            entity.HasKey(e => e.Lid).HasName("PRIMARY");

            entity.Property(e => e.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.Lotteries)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lottery_users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Oid).HasName("PRIMARY");

            entity.Property(e => e.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.LidNavigation).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_lottery");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_users");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.Rid).HasName("PRIMARY");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PRIMARY");

            entity.Property(e => e.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Role).HasDefaultValueSql("'USER'");
        });

        modelBuilder.Entity<WalletTxn>(entity =>
        {
            entity.HasKey(e => e.Wid).HasName("PRIMARY");

            entity.Property(e => e.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TopUp).HasDefaultValueSql("'0.00'");
            entity.Property(e => e.Withdraw).HasDefaultValueSql("'0.00'");

            entity.HasOne(d => d.UidNavigation).WithMany(p => p.WalletTxns)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallettxn_users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
