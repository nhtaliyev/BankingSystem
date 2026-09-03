using BankingSystem.Business.DTOs.TokenDtos;
using BankingSystem.Business.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankingSystem.Business.Interfaces
{
    public interface IAuthService
    {
        Task Register(UserRegisterDto dto);
        Task<ICollection<UserGetDto>> GetAllUsersAsync();
        Task<UserGetDto> GetById(string id);
        Task UpdateUserAsync(string id, UserEditDto dto);
        Task<TokenResponseDto> Login(UserLoginDto dto);

        //Task ForgotPassword(ForgotPasswordDto dto);
    }
}
