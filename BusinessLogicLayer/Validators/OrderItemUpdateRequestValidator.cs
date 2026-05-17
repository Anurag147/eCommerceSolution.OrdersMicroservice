namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;
using FluentValidation;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class OrderItemUpdateRequestValidator : AbstractValidator<OrderItemUpdateRequest>
{
  public OrderItemUpdateRequestValidator()
  {
    RuleFor(x => x.ProductID).NotEmpty().WithErrorCode("ProductIDRequired");
    RuleFor(x => x.UnitPrice).GreaterThan(0).WithErrorCode("UnitPriceGreaterThanZero");
    RuleFor(x => x.Quantity).GreaterThan(0).WithErrorCode("QuantityGreaterThanZero");
  }
}