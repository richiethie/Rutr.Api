using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services.Ai;

/// <summary>
/// Draft-only stand-in for OpenAI until a live key is wired. Evidence refs stay in EvidenceSummary.
/// </summary>
public class MockDraftGenerator
{
    public (string Subject, string Body) Generate(Job job, IReadOnlyList<PhotoEvidence> photos)
    {
        var first = FirstName(job.HomeownerName);
        var subject = photos.Count == 1
            ? "Progress update ready for review"
            : $"{photos.Count} progress updates ready for review";

        var bullets = string.Join(" ", photos.Select(p => p.Caption.TrimEnd('.') + "."));
        var body =
            $"Hi {first} — {bullets} Your project is at stage {job.Stage}. " +
            "We'll share verified photos in your portal after your project manager approves this update.";

        return (subject, body);
    }

    private static string FirstName(string homeownerName)
    {
        var part = homeownerName.Split('&', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(part) ? "there" : part;
    }
}
