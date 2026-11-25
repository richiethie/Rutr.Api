namespace Rutr.Api.Models.Dtos;

public record JobListItemDto(
    Guid Id,
    string Title,
    string JobType,
    string AddressLine1,
    string City,
    string State,
    string HomeownerName,
    string Status,
    string Stage,
    Guid? PropertyId,
    Guid? PrimaryContactId,
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
    Guid JobId,
    string Channel,
    string Subject,
    string Body,
    string Status,
    string EvidenceSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedByStaffId);

public record JobDetailDto(
    Guid Id,
    string Title,
    string JobType,
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
    Guid? PropertyId,
    Guid? PrimaryContactId,
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

public record PortalMilestoneDto(string Name, string Status, DateTimeOffset? CompletedAt);

public record PortalJobDto(
    string OrganizationName,
    string AddressLine1,
    string City,
    string State,
    string HomeownerName,
    string Status,
    string Stage,
    string LatestUpdate,
    IReadOnlyList<PhotoDto> Photos,
    IReadOnlyList<PortalMilestoneDto> Milestones);

public record ApproveDraftRequest(Guid? StaffUserId);

public record AuditEventDto(
    Guid Id,
    Guid? JobId,
    Guid? DraftId,
    string Action,
    string Detail,
    DateTimeOffset CreatedAt);
