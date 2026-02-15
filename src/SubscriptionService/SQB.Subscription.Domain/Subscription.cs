namespace SQB.Subscription.Domain;

public class Subscription
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int SubscriptionPlanId { get; set; }
}
