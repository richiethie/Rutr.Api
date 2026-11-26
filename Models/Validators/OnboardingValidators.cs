using FluentValidation;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Models.Validators;

public class SignupRequestValidator : AbstractValidator<SignupRequest>
{
    public SignupRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.BrandPrimaryColor).MaximumLength(32).When(x => x.BrandPrimaryColor is not null);
        RuleFor(x => x.TimeZoneId).MaximumLength(64).When(x => x.TimeZoneId is not null);
        RuleFor(x => x.QuietHoursStartLocal).InclusiveBetween(0, 23).When(x => x.QuietHoursStartLocal.HasValue);
        RuleFor(x => x.QuietHoursEndLocal).InclusiveBetween(0, 23).When(x => x.QuietHoursEndLocal.HasValue);
    }
}

public class AddStaffMemberRequestValidator : AbstractValidator<AddStaffMemberRequest>
{
    public AddStaffMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Role).NotEmpty().Must(r => r is "Admin" or "Pm" or "Sales")
            .WithMessage("Role must be Admin, Pm, or Sales.");
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}

public class InviteStaffRequestValidator : AbstractValidator<InviteStaffRequest>
{
    public InviteStaffRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).MaximumLength(120).When(x => x.DisplayName is not null);
        RuleFor(x => x.Role).NotEmpty().Must(r => r is "Admin" or "Pm" or "Sales")
            .WithMessage("Role must be Admin, Pm, or Sales.");
    }
}

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.InviteToken).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.DisplayName).MaximumLength(120).When(x => x.DisplayName is not null);
    }
}

public class OnboardingSeedRequestValidator : AbstractValidator<OnboardingSeedRequest>
{
    public OnboardingSeedRequestValidator()
    {
        RuleFor(x => x.Mode).NotEmpty()
            .Must(m => m.Equals("empty", StringComparison.OrdinalIgnoreCase)
                       || m.Equals("sample", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Mode must be empty or sample.");
    }
}

public class OnboardingImportRequestValidator : AbstractValidator<OnboardingImportRequest>
{
    public OnboardingImportRequestValidator()
    {
        RuleFor(x => x.Source).NotEmpty()
            .Must(s => s.Equals("jobnimbus", StringComparison.OrdinalIgnoreCase)
                       || s.Equals("csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source must be jobnimbus or csv.");
        RuleFor(x => x.CsvContent).NotEmpty()
            .When(x => x.Source.Equals("csv", StringComparison.OrdinalIgnoreCase));
    }
}
