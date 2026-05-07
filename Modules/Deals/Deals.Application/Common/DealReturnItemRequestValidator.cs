using Deals.Application.Contracts;
using FluentValidation;

namespace Deals.Application.Common;

internal sealed class DealReturnItemRequestValidator : AbstractValidator<DealReturnItemRequest>
{
    public DealReturnItemRequestValidator()
    {
        RuleFor(x => x.DealItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.StorageId).NotEqual(Guid.Empty).When(x => x.StorageId.HasValue);
    }
}
