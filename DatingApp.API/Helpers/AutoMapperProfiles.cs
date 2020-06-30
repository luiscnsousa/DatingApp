namespace DatingApp.API.Helpers
{
    using System.Linq;
    using AutoMapper;
    using DatingApp.API.Dtos;
    using DatingApp.API.Models;

    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            this.CreateMap<User, ListUserDto>()
                .ForMember(dest => dest.PhotoUrl,
                    opt => opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(dest => dest.Age,
                    opt => opt.MapFrom(src => src.DateOfBirth.CalculateAge()));
            this.CreateMap<User, DetailedUserDto>()
                .ForMember(dest => dest.PhotoUrl,
                    opt => opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(dest => dest.Age,
                    opt => opt.MapFrom(src => src.DateOfBirth.CalculateAge()));
            this.CreateMap<Photo, DetailedPhotoDto>();
            this.CreateMap<UserForUpdateDto, User>();
        }
    }
}