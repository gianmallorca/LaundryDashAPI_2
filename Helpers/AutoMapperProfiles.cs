
using AutoMapper;
using LaundryDashAPI_2.DTOs;
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

            CreateMap<IdentityUser, UserDTO>();
        }
    }
}
