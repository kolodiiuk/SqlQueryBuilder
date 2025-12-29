namespace SqlQueryBuilder.QueryExecution.Enums;

public enum OnDeleteBehavior
{
    Cascade = 0,

    SetNull = 1,

    SetDefault = 2,

    Restrict = 3,

    NoAction = 4,
}
