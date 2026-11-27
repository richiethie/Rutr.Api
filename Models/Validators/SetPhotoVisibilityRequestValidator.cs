using FluentValidation;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Models.Validators;

public class SetPhotoVisibilityRequestValidator : AbstractValidator<SetPhotoVisibilityRequest>
{
    public SetPhotoVisibilityRequestValidator()
    {
        // bool always valid; keep validator registered for pipeline consistency
        RuleFor(x => x).NotNull();
    }
}
