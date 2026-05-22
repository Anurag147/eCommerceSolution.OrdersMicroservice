namespace eCommerce.ProductsMicroservice.BusinessLogicLayer.Mappers;
using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class ProductDTOToOrderItemResponseMappingProfile : Profile
{
  public ProductDTOToOrderItemResponseMappingProfile()
  {
    CreateMap<ProductDTO, OrderItemResponse>()
        .ForMember(dest => dest.ProductID, opt => opt.MapFrom(src => src.ProductID))
        .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category));
  }
}