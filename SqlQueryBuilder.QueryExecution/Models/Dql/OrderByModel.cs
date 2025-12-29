using SqlQueryBuilder.QueryExecution.Enums;

namespace SqlQueryBuilder.QueryExecution.Models.Dql;

public class OrderByModel
{
    public ColumnDqlModel Column { get; set; }

    public OrderDir Dir { get; set; }
}
