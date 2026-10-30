using AutoMapper;
using BankingSystem.Business.DTOs.AccountDTOs;
using BankingSystem.Business.DTOs.CardDTOs;
using BankingSystem.Business.DTOs.TransactionDTOs;
using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Core.Models;

namespace BankingSystem.Business.Mappings
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
            // Entity -> DTO
            CreateMap<Account, AccountGetDto>();
            CreateMap<Card, CardGetDto>();

            CreateMap<Transaction, TransactionGetDto>()
                .ForCtorParam(nameof(TransactionGetDto.CreatedAt), opt => opt.MapFrom(src => src.CreatedTime));

            CreateMap<AppUser, UserGetDto>()
                .ForCtorParam(nameof(UserGetDto.AppUserId), opt => opt.MapFrom(src => src.Id));

            CreateMap<AccountCreateDto, Account>(MemberList.Source);
            CreateMap<CardCreateDto, Card>(MemberList.Source);
        }
    }
}
