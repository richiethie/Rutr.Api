namespace Rutr.Api.Models.Dtos;

public record SyncResultDto(
    bool UseMocks,
    int JobsUpserted,
    int PhotosAdded,
    int DraftsCreated,
    int PortalTokensEnsured,
    string Message);

public record IntegrationStatusDto(
    bool UseMocks,
    string JobNimbus,
    string CompanyCam,
    string Messaging);

public record RejectDraftRequest(string? Reason);

public record SetPhotoVisibilityRequest(bool CustomerVisible);
