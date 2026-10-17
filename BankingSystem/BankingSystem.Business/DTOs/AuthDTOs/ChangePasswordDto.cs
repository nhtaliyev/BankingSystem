namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
}
