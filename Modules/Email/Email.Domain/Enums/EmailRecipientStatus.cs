namespace Email.Domain.Enums;

public enum EmailRecipientStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    SkippedNoEmail = 4,
    SkippedRecentlySent = 5,
    SkippedMarketingDisabled = 6
}
