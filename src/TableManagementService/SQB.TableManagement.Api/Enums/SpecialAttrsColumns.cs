namespace SQB.TableManagement.Api.Enums;

public enum SpecialAttrsColumns
{
    IsIdentity = 0,

    IsPrimaryKey = 1 << 0,

    IsForeignKey = 2 << 0,

    HasCheckConstraintNB = 3 << 0,

    IsNullable = 4 << 0,

    IsUnique = 5 << 0,
}
