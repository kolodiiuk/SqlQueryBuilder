using SqlQueryBuilder.Api.Models;
using SqlQueryBuilder.Api.Models.Ddl;

namespace SqlQueryBuilder.Api.Requests;

public class CreateTableRequest
{
    public IEnumerable<ColumnDdlModel> Columns { get; set; }

    public Guid UserId { get; set; }
}
