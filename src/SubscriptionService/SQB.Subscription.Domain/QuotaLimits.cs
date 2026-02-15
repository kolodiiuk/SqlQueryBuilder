namespace SQB.Subscription.Domain;

public class QuotaLimits
{
    public int MaxTables { get; set; }

    public int MaxQueriesTotal { get; init; }

    public int MaxRowCount { get; init; }

    public int MaxTableCount { get; init; }

    public int MaxFileUploadSizeMB { get; init; }
}
