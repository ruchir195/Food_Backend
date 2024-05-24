using AutoMapper;
using Backend.Dto;
using Backend.Models;

namespace Backend.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<BookingModel, BookingDTO>().ReverseMap();
        }
    }
}
