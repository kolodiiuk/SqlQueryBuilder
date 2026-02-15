using SQB.TableManagement.Api.Enums;

namespace SQB.TableManagement.Api.Models.Ddl;

public class ColumnDdlModel
{
    public string Name { get; set; }

    public string Type { get; set; }

    public int? Size { get; set; }

    public OnDeleteBehavior OnDelete { get; set; }

    public SpecialAttrsColumns SpecialAttrsColumns { get; set; }

    public string ParentColumnName { get; set; }

    public string ParentTableName { get; set; }
}
