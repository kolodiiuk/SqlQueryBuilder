using SQB.DdlDml.Api.Models.Ddl;

namespace SQB.DdlDml.Api.Requests;

public class CreateTableRequest
{
    public IEnumerable<ColumnDdlModel> Columns { get; set; }

    public Guid UserId { get; set; }
}
