namespace Rutr.Api.Models.Dtos;

public record SignupRequest(
    string Email,
    string Password,
    string DisplayName,
    string OrganizationName,
    string? BrandPrimaryColor,
    int? QuietHoursStartLocal,
    int? QuietHoursEndLocal,
    string? TimeZoneId);

public record SignupResponse(
    string Token,
    Guid StaffUserId,
    string DisplayName,
    string Role,
    Guid OrganizationId,
    OrganizationDto Organization);

public record AddStaffMemberRequest(
    string Email,
    string DisplayName,
    string Role,
    string TemporaryPassword);

public record InviteStaffRequest(
    string Email,
    string? DisplayName,
    string Role);

public record InviteDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    string InviteToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public record AcceptInviteRequest(
    string InviteToken,
    string Password,
    string? DisplayName);

public record OnboardingSeedRequest(string Mode);

public record OnboardingSeedResult(
    string Mode,
    int ContactsCreated,
    int PropertiesCreated,
    int JobsCreated,
    string Message);

public record OnboardingImportRequest(
    string Source,
    string? CsvContent,
    bool DryRun = false);

/// <summary>Unmatched / invalid row detail for Web import summary.</summary>
public record OnboardingImportError(
    int? RowNumber,
    string? ExternalId,
    string Code,
    string Message);

public record OnboardingImportResult(
    string Source,
    bool DryRun,
    int ContactsCreated,
    int PropertiesCreated,
    int JobsCreated,
    int Skipped,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<OnboardingImportError> Errors,
    string IdempotencyRule,
    string Message);

/// <summary>Multipart form for evidence upload (keeps Swagger/OpenAPI healthy).</summary>
public class EvidenceUploadForm
{
    public Guid JobId { get; set; }
    public string Caption { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public string? ExternalCompanyCamId { get; set; }
    public bool CustomerVisible { get; set; }
}
