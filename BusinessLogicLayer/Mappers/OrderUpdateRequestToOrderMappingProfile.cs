namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;


public class OrderUpdateRequestToOrderMappingProfile : Profile
{
  public OrderUpdateRequestToOrderMappingProfile()
  {
    CreateMap<OrderUpdateRequest, Order>()
      .ForMember(dest => dest.OrderID, opt => opt.MapFrom(src => src.OrderID))
      .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
      .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
      .ForMember(dest => dest.UserID, opt => opt.MapFrom(src => src.UserID));
  }
}