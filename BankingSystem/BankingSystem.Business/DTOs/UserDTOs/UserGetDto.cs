namespace BankingSystem.Business.DTOs.UserDTOs
{
    public record UserGetDto(string AppUserId, string FullName, string Email, string PhoneNumber, DateTime Birthday);
}
