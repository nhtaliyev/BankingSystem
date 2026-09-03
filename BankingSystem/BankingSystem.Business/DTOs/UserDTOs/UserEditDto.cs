namespace BankingSystem.Business.DTOs.UserDTOs
{
    public record UserEditDto(string FullName, string Email, string PhoneNumber, DateTime Birthday);
}
