using SQB.TableManagement.Api.Models.Ddl;

namespace SQB.TableManagement.Api.Requests;

public class CreateTableRequest
{
    public IEnumerable<ColumnDdlModel> Columns { get; set; }

    public Guid UserId { get; set; }
}
