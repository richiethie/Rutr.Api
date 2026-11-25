namespace Rutr.Api.Models.Dtos;

public record JobListItemDto(
    Guid Id,
    string AddressLine1,
    string City,
    string State,
    string HomeownerName,
    string Status,
    string Stage,
    int PendingApprovals,
    int PhotoCount,
    DateTimeOffset UpdatedAt);

public record PhotoDto(
    Guid Id,
    string Caption,
    string ImageUrl,
    bool CustomerVisible,
    DateTimeOffset CapturedAt);

public record MessageDraftDto(
    Guid Id,
    string Channel,
    string Subject,
    string Body,
    string Status,
    string EvidenceSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);

public record JobDetailDto(
    Guid Id,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string HomeownerName,
    string HomeownerEmail,
    string HomeownerPhone,
    string Status,
    string Stage,
    string ExternalJobNimbusId,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PhotoDto> Photos,
    IReadOnlyList<MessageDraftDto> Drafts,
    string? PortalToken);

public record ApprovalQueueItemDto(
    Guid DraftId,
    Guid JobId,
    string AddressLine1,
    string HomeownerName,
    string Channel,
    string Subject,
    string Body,
    string EvidenceSummary,
    DateTimeOffset CreatedAt);

public record PortalJobDto(
    string OrganizationName,
    string AddressLine1,
    string City,
    string State,
    string HomeownerName,
    string Status,
    string Stage,
    string LatestUpdate,
    IReadOnlyList<PhotoDto> Photos);

public record ApproveDraftRequest(Guid? StaffUserId);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string DisplayName, string Role, Guid OrganizationId);
