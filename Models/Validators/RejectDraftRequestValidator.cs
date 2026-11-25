using FluentValidation;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Models.Validators;

public class RejectDraftRequestValidator : AbstractValidator<RejectDraftRequest>
{
    public RejectDraftRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
