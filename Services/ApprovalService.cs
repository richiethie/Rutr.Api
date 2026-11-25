using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class ApprovalService(RutrDbContext db)
{
    public async Task<IReadOnlyList<ApprovalQueueItemDto>> QueueAsync(Guid organizationId, CancellationToken ct = default)
    {
        var rows = await db.MessageDrafts
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId && d.Status == "PendingApproval")
            .Select(d => new ApprovalQueueItemDto(
                d.Id,
                d.JobId,
                d.Job.AddressLine1,
                d.Job.HomeownerName,
                d.Channel,
                d.Subject,
                d.Body,
                d.EvidenceSummary,
                d.CreatedAt))
            .ToListAsync(ct);

        return rows.OrderBy(d => d.CreatedAt).ToList();
    }

    public async Task<MessageDraftDto?> ApproveAsync(
        Guid organizationId,
        Guid draftId,
        Guid staffUserId,
        CancellationToken ct = default)
    {
        var draft = await db.MessageDrafts
            .Include(d => d.Job)
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Id == draftId, ct);

        if (draft is null || draft.Status != "PendingApproval")
        {
            return null;
        }

        draft.Status = "Approved";
        draft.ApprovedAt = DateTimeOffset.UtcNow;
        draft.ApprovedByStaffId = staffUserId;
        draft.Job.UpdatedAt = DateTimeOffset.UtcNow;

        // Mark related evidence customer-visible on approve (Connect MVP behavior).
        var photos = await db.Photos
            .Where(p => p.JobId == draft.JobId && !p.CustomerVisible)
            .ToListAsync(ct);
        foreach (var photo in photos)
        {
            photo.CustomerVisible = true;
        }

        await db.SaveChangesAsync(ct);

        return new MessageDraftDto(
            draft.Id,
            draft.Channel,
            draft.Subject,
            draft.Body,
            draft.Status,
            draft.EvidenceSummary,
            draft.CreatedAt,
            draft.ApprovedAt);
    }
}
