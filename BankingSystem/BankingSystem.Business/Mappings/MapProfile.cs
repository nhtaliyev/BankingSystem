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
            CreateMap<Account, AccountGetDto>();
            CreateMap<Card, CardGetDto>();

            CreateMap<Transaction, TransactionGetDto>()
                .ForCtorParam(nameof(TransactionGetDto.CreatedAt), opt => opt.MapFrom(src => src.CreatedTime));

            CreateMap<AppUser, UserGetDto>()
                .ForCtorParam(nameof(UserGetDto.AppUserId), opt => opt.MapFrom(src => src.Id));

            CreateMap<CardEditDto, Card>(MemberList.Source)
                .ForMember(d => d.RowVersion, o => o.Ignore())
                .ForSourceMember(s => s.RowVersion, o => o.DoNotValidate());

            CreateMap<UserEditDto, AppUser>(MemberList.Source)
                .ForMember(d => d.Email, o => o.Ignore())
                .ForMember(d => d.PhoneNumber, o => o.Ignore())
                .ForSourceMember(s => s.Email, o => o.DoNotValidate())
                .ForSourceMember(s => s.PhoneNumber, o => o.DoNotValidate());
        }
    }
}