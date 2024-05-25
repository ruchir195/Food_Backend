using AutoMapper;
using Backend.Dto;
using Backend.Models;

namespace Backend.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<BookingDTO, BookingModel>()
            .ForMember(dest => dest.UserID, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
        }
    }
}
