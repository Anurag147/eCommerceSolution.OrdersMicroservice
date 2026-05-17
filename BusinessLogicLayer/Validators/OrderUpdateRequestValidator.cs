namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;
using FluentValidation;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class OrderUpdateRequestValidator : AbstractValidator<OrderUpdateRequest>
{
  public OrderUpdateRequestValidator()
  {
    RuleFor(x => x.UserID).NotEmpty().WithErrorCode("UserIDRequired");
    RuleFor(x => x.OrderItems).NotEmpty().WithErrorCode("OrderItemsRequired");
    RuleFor(x => x.OrderDate).NotEmpty().WithErrorCode("OrderDateRequired");
    RuleFor(x => x.OrderID).NotEmpty().WithErrorCode("OrderIDRequired");
  }
}