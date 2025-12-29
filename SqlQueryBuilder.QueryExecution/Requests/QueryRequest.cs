using SqlQueryBuilder.QueryExecution.Models.Dql;

namespace SqlQueryBuilder.QueryExecution.Requests;

public class QueryRequest
{
    public SelectModel SelectModel { get; set; }

    public FromModel FromModel { get; set; }

    public WhereModel WhereModel { get; set; }

    public JoinModel JoinModel { get; set; }

    public GroupByModel GroupByModel { get; set; }

    public HavingModel HavingModel { get; set; }

    public OrderByModel OrderByModel { get; set; }

    public int Limit { get; set; }

    public bool Distinct { get; set; }

    public PageModel Page { get; set; }
}
