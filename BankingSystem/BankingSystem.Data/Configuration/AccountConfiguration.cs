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
            .IsRequired()
            .HasMaxLength(20);

            builder.HasIndex(a => a.AccountNumber)
                .IsUnique();

            builder.Property(a => a.Currency)
                .IsRequired();

            builder.Property(a => a.Status)
                .IsRequired();

            builder.Property(a => a.AppUserId)
                .IsRequired();

            builder.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.Cards)
                .WithOne(c => c.Account)
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
