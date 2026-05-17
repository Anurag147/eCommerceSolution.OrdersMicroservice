namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;

public class OrderItemAddRequestToOrderItemMappingProfile : Profile
{
  public OrderItemAddRequestToOrderItemMappingProfile()
  {
    CreateMap<OrderItemAddRequest, OrderItem>()
      .ForMember(dest => dest._id, opt => opt.Ignore())
      .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
      .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => src.ProductID))
      .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
      .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice));
  }
}