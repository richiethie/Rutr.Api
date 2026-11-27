namespace Rutr.Api.Models.Dtos;

public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string BrandPrimaryColor,
    int QuietHoursStartLocal,
    int QuietHoursEndLocal,
    string TimeZoneId);

public record OrganizationUpdateRequest(
    string Name,
    string? BrandPrimaryColor,
    int? QuietHoursStartLocal,
    int? QuietHoursEndLocal,
    string? TimeZoneId);

public record StaffMemberDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid OrganizationId);

public record StaffRoleUpdateRequest(string Role);

public record ContactDto(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    string Phone,
    string Role,
    string PreferredChannel,
    string? Language,
    DateTimeOffset UpdatedAt);

public record ContactWriteRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Role,
    string? PreferredChannel,
    string? Language);

public record PropertyDto(
    Guid Id,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string PropertyType,
    int? YearBuilt,
    string? Notes,
    double? Latitude,
    double? Longitude,
    int JobCount,
    DateTimeOffset UpdatedAt);

public record PropertyWriteRequest(
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string? PropertyType,
    int? YearBuilt,
    string? Notes,
    double? Latitude,
    double? Longitude);

public record JobWriteRequest(
    string? Title,
    string? JobType,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string HomeownerName,
    string HomeownerEmail,
    string HomeownerPhone,
    string? Status,
    string? Stage,
    string? ExternalJobNimbusId,
    Guid? PropertyId,
    Guid? PrimaryContactId);

public record JobStatusUpdateRequest(string Status, string? Stage);

public record NoteDto(
    Guid Id,
    Guid? JobId,
    Guid? ContactId,
    string Body,
    Guid? CreatedByStaffId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record NoteWriteRequest(Guid? JobId, Guid? ContactId, string Body);

public record TaskDto(
    Guid Id,
    Guid? JobId,
    Guid? ContactId,
    string Title,
    string Description,
    string Status,
    DateTimeOffset? DueAt,
    Guid? AssignedStaffId,
    DateTimeOffset UpdatedAt);

public record TaskWriteRequest(
    Guid? JobId,
    Guid? ContactId,
    string Title,
    string? Description,
    string? Status,
    DateTimeOffset? DueAt,
    Guid? AssignedStaffId);

public record MilestoneDto(
    Guid Id,
    Guid JobId,
    string Name,
    string Status,
    int SortOrder,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset UpdatedAt);

public record MilestoneWriteRequest(
    Guid JobId,
    string Name,
    string? Status,
    int? SortOrder,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt);

public record EvidenceDto(
    Guid Id,
    Guid JobId,
    string ExternalCompanyCamId,
    string Caption,
    string ImageUrl,
    bool CustomerVisible,
    DateTimeOffset CapturedAt,
    bool SoftDeleted);

public record EvidenceWriteRequest(
    Guid JobId,
    string Caption,
    string ImageUrl,
    string? ExternalCompanyCamId,
    bool? CustomerVisible,
    DateTimeOffset? CapturedAt);

public record DraftWriteRequest(
    Guid JobId,
    string Channel,
    string Subject,
    string Body,
    string? EvidenceSummary,
    string? Status);

public record ConsentDto(
    Guid Id,
    Guid? JobId,
    Guid? ContactId,
    bool SmsOptIn,
    bool EmailOptIn,
    bool SmsStopped,
    string Source,
    string? DisclosureText,
    DateTimeOffset UpdatedAt);

public record ConsentWriteRequest(
    Guid? JobId,
    Guid? ContactId,
    bool SmsOptIn,
    bool EmailOptIn,
    bool SmsStopped,
    string? Source,
    string? DisclosureText);

public record IntegrationConnectionDto(
    Guid Id,
    string Provider,
    string DisplayName,
    bool IsConnected,
    DateTimeOffset? ConnectedAt,
    DateTimeOffset UpdatedAt);
