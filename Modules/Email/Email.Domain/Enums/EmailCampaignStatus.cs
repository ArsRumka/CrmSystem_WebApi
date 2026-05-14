namespace Email.Domain.Enums;

public enum EmailCampaignStatus
{
    Draft = 1,
    Sending = 2,
    Sent = 3,
    PartiallyFailed = 4,
    Failed = 5,
    Cancelled = 6
}
