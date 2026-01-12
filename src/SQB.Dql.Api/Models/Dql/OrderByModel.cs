using SQB.Dql.Api.Enums;

namespace SQB.Dql.Api.Models.Dql;

public class OrderByModel
{
    public ColumnDqlModel Column { get; set; }

    public OrderDir Dir { get; set; }
}
