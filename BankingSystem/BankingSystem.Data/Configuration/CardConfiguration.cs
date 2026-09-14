using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSystem.Data.Configuration
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.Property(c => c.CardNumberLast4)
                .HasMaxLength(4)
                .IsFixedLength()
                .IsRequired();

            builder.Property(c => c.CardNumberHash)
                .HasMaxLength(64) // hex-encoded SHA256/HMAC-SHA256 output = 64 chars
                .IsRequired();

            builder.Property(c => c.CardNumberEncrypted)
                .HasMaxLength(256) // AES output is longer than the raw input; give it room
                .IsRequired();

            builder.HasIndex(c => c.CardNumberHash)
                .IsUnique();

            builder.Property(c => c.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.DailyLimit)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(c => c.Account)
                .WithMany(a => a.Cards)
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
