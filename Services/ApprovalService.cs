using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Services.Messaging;

namespace Rutr.Api.Services;

public class ApprovalService(RutrDbContext db, AuditService audit, MockDigestSender sender)
{
    public async Task<IReadOnlyList<ApprovalQueueItemDto>> QueueAsync(Guid organizationId, CancellationToken ct = default)
    {
        // Include Approved so PMs can Approve → Send on /approvals without the row vanishing.
        var rows = await db.MessageDrafts
            .AsNoTracking()
            .Where(d =>
                d.OrganizationId == organizationId
                && (d.Status == "PendingApproval" || d.Status == "Approved"))
            .Select(d => new ApprovalQueueItemDto(
                d.Id,
                d.JobId,
                d.Job.AddressLine1,
                d.Job.HomeownerName,
                d.Channel,
                d.Subject,
                d.Body,
                d.EvidenceSummary,
                d.Status,
                d.CreatedAt))
            .ToListAsync(ct);

        return rows
            .OrderBy(d => d.Status == "PendingApproval" ? 0 : 1)
            .ThenBy(d => d.CreatedAt)
            .ToList();
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

        // Connect MVP: approving a digest makes related not-yet-visible evidence customer-visible.
        var photos = await db.Photos
            .Where(p => p.JobId == draft.JobId && !p.CustomerVisible)
            .ToListAsync(ct);
        foreach (var photo in photos)
        {
            photo.CustomerVisible = true;
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(
            organizationId,
            "draft.approved",
            $"Approved draft '{draft.Subject}' and marked {photos.Count} photo(s) customer-visible.",
            draft.JobId,
            draft.Id,
            staffUserId,
            ct);

        return ToDto(draft);
    }

    public async Task<MessageDraftDto?> RejectAsync(
        Guid organizationId,
        Guid draftId,
        Guid staffUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var draft = await db.MessageDrafts
            .Include(d => d.Job)
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Id == draftId, ct);

        if (draft is null || draft.Status != "PendingApproval")
        {
            return null;
        }

        draft.Status = "Rejected";
        draft.Job.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(
            organizationId,
            "draft.rejected",
            string.IsNullOrWhiteSpace(reason) ? "Rejected without reason." : reason.Trim(),
            draft.JobId,
            draft.Id,
            staffUserId,
            ct);

        return ToDto(draft);
    }

    public async Task<MessageDraftDto?> SendAsync(
        Guid organizationId,
        Guid draftId,
        Guid staffUserId,
        CancellationToken ct = default)
    {
        var draft = await db.MessageDrafts
            .Include(d => d.Job)
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Id == draftId, ct);

        if (draft is null || draft.Status != "Approved")
        {
            return null;
        }

        var sendResult = await sender.SendAsync(draft, draft.Job, ct);
        if (!sendResult.Sent)
        {
            await audit.WriteAsync(
                organizationId,
                "draft.send_blocked",
                sendResult.Detail,
                draft.JobId,
                draft.Id,
                staffUserId,
                ct);
            throw new InvalidOperationException(sendResult.Detail);
        }

        draft.Status = "Sent";
        draft.Job.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(
            organizationId,
            "draft.sent",
            sendResult.Detail,
            draft.JobId,
            draft.Id,
            staffUserId,
            ct);

        return ToDto(draft);
    }

    private static MessageDraftDto ToDto(Models.Entities.MessageDraft draft) =>
        new(draft.Id, draft.JobId, draft.Channel, draft.Subject, draft.Body, draft.Status, draft.EvidenceSummary,
            draft.CreatedAt, draft.ApprovedAt, draft.ApprovedByStaffId);
}
