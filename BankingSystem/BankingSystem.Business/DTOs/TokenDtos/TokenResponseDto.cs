namespace BankingSystem.Business.DTOs.TokenDtos
{
    public record TokenResponseDto(string AccessToken, DateTime ExpireDate);
}
