namespace Rutr.Api.Configuration;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>Local hour (0-23) when outbound digests may start.</summary>
    public int QuietHoursStartLocal { get; set; } = 8;

    /// <summary>Local hour (0-23) when outbound digests must stop (exclusive).</summary>
    public int QuietHoursEndLocal { get; set; } = 20;

    /// <summary>Windows ID preferred locally; Linux containers can override to America/Chicago.</summary>
    public string TimeZoneId { get; set; } = "Central Standard Time";
}
