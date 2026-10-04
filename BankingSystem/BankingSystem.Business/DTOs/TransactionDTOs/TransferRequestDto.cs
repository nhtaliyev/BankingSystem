namespace BankingSystem.Business.DTOs.TransactionDTOs
{
    public record TransferRequestDto(int? FromAccountId, int? FromCardId, int? ToAccountId, int? ToCardId,
                            decimal Amount, string? Description);
}

