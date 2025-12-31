using SQB.QueryExecution.Enums;

namespace SQB.QueryExecution.Models.Dql;

public class OrderByModel
{
    public ColumnDqlModel Column { get; set; }

    public OrderDir Dir { get; set; }
}
