
using AutoMapper;
using LaundryDashAPI_2.DTOs.AppUser;
using LaundryDashAPI_2.DTOs.LaundryServiceLog;
using LaundryDashAPI_2.DTOs.LaundryShop;
using LaundryDashAPI_2.DTOs.Service;
using LaundryDashAPI_2.Entities;
using Microsoft.AspNetCore.Identity;

namespace LaundryDashAPI_2.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<ServiceDTO, Service>().ReverseMap();
            CreateMap<ServiceCreationDTO, Service>();

            CreateMap<LaundryShopDTO, LaundryShop>().ReverseMap();
            CreateMap<LaundryShopCreationDTO, LaundryShop>();

            CreateMap<LaundryServiceLog, LaundryServiceLogDTO>();
            CreateMap<LaundryServiceLogCreationDTO, LaundryServiceLog>()
                .ForMember(dest => dest.ServiceIds, opt => opt.MapFrom(src => src.ServiceIds)); // Ensure ServiceIds is mapped

            CreateMap<IdentityUser, ApplicationUserDTO>();
        }
    }
}
