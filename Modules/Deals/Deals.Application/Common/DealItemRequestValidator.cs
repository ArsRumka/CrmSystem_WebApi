using Deals.Application.Contracts;
using Deals.Domain.Enums;
using FluentValidation;

namespace Deals.Application.Common;

internal sealed class DealItemRequestValidator : AbstractValidator<DealItemRequest>
{
    public DealItemRequestValidator()
    {
        RuleFor(x => x.ItemType).IsInEnum();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.StorageId).NotEqual(Guid.Empty).When(x => x.StorageId.HasValue);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ManualDiscountType).IsInEnum().When(x => x.ManualDiscountType.HasValue);
        RuleFor(x => x)
            .Must(x => x.ItemType != DealItemType.Product || x.StorageId.HasValue)
            .WithMessage("StorageId is required for product items");
        RuleFor(x => x)
            .Must(x => x.ItemType != DealItemType.Service || x.StorageId is null)
            .WithMessage("StorageId must be null for service items");
        RuleFor(x => x)
            .Must(IsValidManualDiscount)
            .WithMessage("Invalid manual discount rule");
    }

    private static bool IsValidManualDiscount(DealItemRequest request)
    {
        if (!request.ManualDiscountType.HasValue)
        {
            return !request.ManualDiscountValue.HasValue || request.ManualDiscountValue.Value == 0;
        }

        return request.ManualDiscountType.Value switch
        {
            DealDiscountType.None => !request.ManualDiscountValue.HasValue || request.ManualDiscountValue.Value == 0,
            DealDiscountType.Percent => request.ManualDiscountValue.HasValue &&
                                        request.ManualDiscountValue.Value > 0 &&
                                        request.ManualDiscountValue.Value <= 100,
            DealDiscountType.Fixed => request.ManualDiscountValue.HasValue &&
                                      request.ManualDiscountValue.Value > 0,
            _ => false
        };
    }
}
