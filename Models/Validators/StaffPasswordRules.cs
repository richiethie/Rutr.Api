using System.Text.RegularExpressions;
using FluentValidation;

namespace Rutr.Api.Models.Validators;

/// <summary>
/// Shared staff password strength for signup / accept-invite / admin-add temp password
/// (aligned with Web confirm-password UI). confirmPassword is client-only.
/// </summary>
public static class StaffPasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public const string MinLengthMessage = "Password must be at least 8 characters.";
    public const string LetterMessage = "Password must include at least one letter.";
    public const string NumberMessage = "Password must include at least one number.";
    public const string SpecialMessage = "Password must include at least one special character.";

    private static readonly Regex HasLetter = new("[A-Za-z]", RegexOptions.Compiled);
    private static readonly Regex HasNumber = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex HasSpecial = new("[^A-Za-z0-9]", RegexOptions.Compiled);

    public static bool MeetsStrength(string? password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= MinLength
        && password.Length <= MaxLength
        && HasLetter.IsMatch(password)
        && HasNumber.IsMatch(password)
        && HasSpecial.IsMatch(password);

    public static IRuleBuilderOptions<T, string> StaffPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinLength).WithMessage(MinLengthMessage)
            .MaximumLength(MaxLength)
            .Must(p => HasLetter.IsMatch(p)).WithMessage(LetterMessage)
            .Must(p => HasNumber.IsMatch(p)).WithMessage(NumberMessage)
            .Must(p => HasSpecial.IsMatch(p)).WithMessage(SpecialMessage);
}
