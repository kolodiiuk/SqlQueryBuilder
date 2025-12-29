using SqlQueryBuilder.QueryExecution.Models.Ddl;

namespace SqlQueryBuilder.QueryExecution.Requests;

public class CreateTableRequest
{
    public IEnumerable<ColumnDdlModel> Columns { get; set; }

    public Guid UserId { get; set; }
}
