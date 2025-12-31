using SQB.QueryExecution.Models.Ddl;

namespace SQB.QueryExecution.Requests;

public class CreateTableRequest
{
    public IEnumerable<ColumnDdlModel> Columns { get; set; }

    public Guid UserId { get; set; }
}
