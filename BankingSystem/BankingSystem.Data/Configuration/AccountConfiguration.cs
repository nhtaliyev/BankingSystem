using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSystem.Data.Configuration
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.Property(a => a.AccountNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(a => a.AccountNumber)
                .IsUnique();

            builder.Property(a => a.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Property(a => a.Currency)
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(a => a.User)
                .WithMany() // add AppUser.Accounts collection later if you need it
                .HasForeignKey(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
