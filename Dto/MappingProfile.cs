using AutoMapper;
using GemApi.Dto.Request;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;

namespace GemApi.DTOs
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<GeMbidExtract, BidListDto>()
                .ForMember(dest => dest.IsActive,
                    opt => opt.MapFrom(src => src.BidEndDateTime >= DateTime.Now))
                .ForMember(dest => dest.IsClosingSoon,
                    opt => opt.MapFrom(src =>
                        src.BidEndDateTime >= DateTime.Now &&
                        src.BidEndDateTime <= DateTime.Now.AddDays(3)));

            CreateMap<GeMbidExtract, BidDetailDto>();
            CreateMap<Admin,LoginDto>();
        }
    }
}