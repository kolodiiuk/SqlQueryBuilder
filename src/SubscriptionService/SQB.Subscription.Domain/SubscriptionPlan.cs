namespace SQB.Subscription.Domain;

public class SubscriptionPlan
{
    public int Id { get; set; }

    public QuotaLimits QuotaLimits { get; set; }
}
