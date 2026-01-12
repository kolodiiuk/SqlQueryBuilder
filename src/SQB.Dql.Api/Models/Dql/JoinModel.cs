using SQB.Dql.Api.Enums;

namespace SQB.Dql.Api.Models.Dql;

public class JoinModel
{
    public JoinType Type { get; set; }

    public string LeftTableName { get; set; }

    public string RightTableName { get; set; }

    public JoinOp Op { get; set; }

    public string LeftTableColumn { get; set; }

    public string RightTableColumn { get; set; }
}
