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
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Currency)
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(200);

            builder.HasOne(t => t.FromCard)
                .WithMany(c => c.SentTransactions)
                .HasForeignKey(t => t.FromCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ToCard)
                .WithMany(c => c.ReceivedTransactions)
                .HasForeignKey(t => t.ToCardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.FromAccount)
                .WithMany()
                .HasForeignKey(t => t.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ToAccount)
                .WithMany()
                .HasForeignKey(t => t.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Defense-in-depth: DB-level guarantee that a side never references both an account and a card.
            // Primary enforcement should still happen in TransactionService before SaveChangesAsync.
            builder.ToTable(tb => tb.HasCheckConstraint(
                "CK_Transaction_FromSide",
                "(\"FromAccountId\" IS NULL OR \"FromCardId\" IS NULL)"));

            builder.ToTable(tb => tb.HasCheckConstraint(
                "CK_Transaction_ToSide",
                "(\"ToAccountId\" IS NULL OR \"ToCardId\" IS NULL)"));
        }
    }
}
