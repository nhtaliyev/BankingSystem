using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSystem.Data.Configuration
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.Property(c => c.CardNumber)
                .IsRequired()
                .HasMaxLength(16);

            builder.HasIndex(c => c.CardNumber)
                .IsUnique();

            builder.Property(c => c.ExpiryDate)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired();

            builder.Property(c => c.Balance)
                .HasPrecision(18, 2);

            builder.Property(c => c.DailyLimit)
                .HasPrecision(18, 2);

            builder.HasOne(c => c.Account)
                .WithMany(a => a.Cards)
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
