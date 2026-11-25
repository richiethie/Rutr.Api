using Microsoft.EntityFrameworkCore;
using Rutr.Api.Authentication;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class AuthService(RutrDbContext db, JwtTokenService tokens)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var hash = SeedData.HashPassword(request.Password);
        var user = await db.StaffUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == hash, ct);

        if (user is null)
        {
            return null;
        }

        return new LoginResponse(tokens.CreateToken(user), user.DisplayName, user.Role, user.OrganizationId);
    }

    public async Task<StaffMeDto?> GetMeAsync(Guid staffUserId, CancellationToken ct = default)
    {
        var user = await db.StaffUsers
            .AsNoTracking()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == staffUserId, ct);

        if (user is null)
        {
            return null;
        }

        return new StaffMeDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.OrganizationId,
            user.Organization.Name);
    }
}
