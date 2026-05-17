namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

using FluentValidation;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
{
  public OrderAddRequestValidator()
  {
    RuleFor(x => x.UserID).NotEmpty().WithErrorCode("UserIDRequired");
    RuleFor(x => x.OrderItems).NotEmpty().WithErrorCode("OrderItemsRequired");
    RuleFor(x=>x.OrderDate).NotEmpty().WithErrorCode("OrderDateRequired");
  }
}