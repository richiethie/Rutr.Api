namespace Rutr.Api.Models.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string DisplayName, string Role, Guid OrganizationId);

public record StaffMeDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid OrganizationId,
    string OrganizationName);
