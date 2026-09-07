using BankingSystem.Business.DTOs.TokenDtos;
using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankingSystem.Business.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<ICollection<UserGetDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            var userDtos = users.Select(user => new UserGetDto(
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Birthday
            )).ToList();

            return userDtos;
        }

        //public async Task<ICollection<UserGetDto>> GetAllAdminsAsync()
        //{
        //    var users = await _userManager.GetUsersInRoleAsync("Admin");

        //    var userDtos = users.Select(user => new UserGetDto(
        //        user.Id,
        //        user.FullName,
        //        user.Email,
        //        user.PhoneNumber,
        //        user.Status,
        //        user.Birthday,
        //        user.Gender
        //    )).ToList();

        //    return userDtos;
        //}

        public async Task UpdateUserAsync(string id, UserEditDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                throw new NotFoundException($"User not found");
            }

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Status = dto.Status;
            user.Birthday = dto.Birthday;
            user.Gender = dto.Gender;

            if (user.Birthday > DateTime.Now)
            {
                throw new Exceptions.InvalidDataException("Invalid Birthday");
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new ValidationException($"Failed to update");
            }
        }

        public async Task<UserGetDto> GetById(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id.ToString());

            if (user == null)
            {
                throw new NotFoundException($"User not found");
            }

            var userDto = new UserGetDto(
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Status,
                user.Birthday,
                user.Gender
            );

            return userDto;
        }

        public async Task<TokenResponseDto> Login(UserLoginDto dto)
        {
            AppUser appUser = null;

            appUser = await _userManager.FindByEmailAsync(dto.Email);

            if (appUser == null)
            {
                throw new NotFoundException("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, dto.RememberMe);

            if (!result.Succeeded)
            {
                throw new UnauthorizedException("Invalid credentials");
            }

            //if (appUser.Status == AdminStatus.Pending || appUser.Status == AdminStatus.Rejected)
            //{
            //    throw new UnauthorizedException("you must be confirmed as an admin");
            //}

            //var roles = await _userManager.GetRolesAsync(appUser);

            //if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
            //{
            //    throw new UnauthorizedException("You must be an admin to log in");
            //}

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id),
                new Claim(ClaimTypes.Name, appUser.FullName),
            };

            if (roles.Contains("Admin"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            if (roles.Contains("SuperAdmin"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }

            claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
            DateTime expiredt = DateTime.UtcNow.AddHours(6);
            string secretkey = _configuration.GetSection("JWT:secretKey").Value;

            SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey));
            SigningCredentials signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                audience: _configuration.GetSection("JWT:audience").Value,
                issuer: _configuration.GetSection("JWT:issuer").Value,
                expires: expiredt,
                notBefore: DateTime.UtcNow
                );

            string token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            return new TokenResponseDto(token, expiredt);
        }

        public async Task Register(UserRegisterDto dto)
        {
            AppUser appUser = new AppUser()
            {
                Email = dto.Email,
                Birthday = dto.Birthday,
                Gender = dto.Gender,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                UserName = dto.Email,
            };

            if (appUser.Birthday >= DateTime.Now)
                throw new Exceptions.InvalidDataException("Invalid Birth Date");

            var usedemail = await _userManager.FindByEmailAsync(appUser.Email);

            if (usedemail != null)
                throw new ValidationException("This email is already used");

            var usedphone = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == dto.PhoneNumber);

            if (usedphone != null)
                throw new ValidationException("This phone number is already used");
            

            var result = await _userManager.CreateAsync(appUser, dto.Password);

            if (!result.Succeeded)
            {
                throw new ValidationException("Something went wrong");
            }

            var member = await _userManager.FindByEmailAsync(dto.Email);

            if (member is not null)
            {
                await _userManager.AddToRoleAsync(appUser, "Admin");
            }

        }
    }
}
