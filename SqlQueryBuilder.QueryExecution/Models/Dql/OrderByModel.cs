using SqlQueryBuilder.Api.Enums;

namespace SqlQueryBuilder.Api.Models.Dql;

public class OrderByModel
{
    public ColumnDqlModel Column { get; set; }

    public OrderDir Dir { get; set; }
}
