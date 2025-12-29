namespace SqlQueryBuilder.QueryExecution.Models.Dql;

public class ColumnDqlModel
{
    public string TableName { get; set; }

    public string ColumnName { get; set; }

    public string Alias { get; set; } = "";
}
