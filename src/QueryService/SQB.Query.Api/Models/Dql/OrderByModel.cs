using SQB.Query.Api.Enums;

namespace SQB.Query.Api.Models.Dql;

public class OrderByModel
{
    public ColumnDqlModel Column { get; set; }

    public OrderDir Dir { get; set; }
}
