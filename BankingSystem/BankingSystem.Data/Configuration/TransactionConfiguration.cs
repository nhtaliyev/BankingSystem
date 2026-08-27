using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingSystem.Data.Configuration
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.Property(t => t.Amount)
                .HasPrecision(18, 2);

            builder.Property(t => t.Currency)
                .IsRequired();

            builder.Property(t => t.Type)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired();

            builder.HasOne(t => t.FromCard)
                .WithMany()
                .HasForeignKey(t => t.FromCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ToCard)
                .WithMany()
                .HasForeignKey(t => t.ToCardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
