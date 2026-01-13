namespace SQB.Compiler;

public class CsvSqlParser
{
    private readonly FileStream _fileStream;

    private readonly string _name;

    private readonly string _schemaName;

    private readonly string _tableName;

    private CsvSqlParser(FileStream fileStream, string name)
    {
        _fileStream = fileStream;
        _name = name;
    }

    private CsvSqlParser(FileStream fileStream, string schemaName, string tableName)
    {
        _fileStream = fileStream;
        _schemaName = schemaName;
        _tableName = tableName;
    }

    private bool HasChildTables { get; set; }

    public static CsvSqlParser CreateParserCsvOneTable(FileDescription desc, string schemaName)
    {
        ValidateFileDescription(desc);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        return new CsvSqlParser(desc.Stream, desc.FileName);
    }

    public static CsvSqlParser CreateCsvSqlParserManyTables(FileDescription desc, string schemaName)
    {
        ValidateFileDescription(desc);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        var parser = new CsvSqlParser(desc.Stream, desc.FileName);
        parser.HasChildTables = true;

        return parser;
    }

    public static CsvSqlParser CreateCsvSqlParserExistingTable(FileDescription desc, string schemaName,
        string tableName)
    {
        ValidateFileDescription(desc);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        var parser = new CsvSqlParser(desc.Stream, schemaName, tableName);

        return parser;
    }

    public IEnumerable<ColumnModel> ParseColumns()
    {
        throw new NotImplementedException();
    }

    public void ParseValues()
    {
        throw new NotImplementedException();
    }

    private string[] ParseColumnNames()
    {
        throw new NotImplementedException();
    }

    private string[] ParseColumnTypes()
    {
        throw new NotImplementedException();
    }

    private static void ValidateFileDescription(FileDescription desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.Stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(desc.FileName);
        if (desc.FileName.Length > 63)
        {
            throw new ArgumentException("Length of table name can't be greater than 63.");
        }

        if (!desc.Stream.CanRead && !desc.Stream.CanSeek)
        {
            throw new ArgumentException("Stream is not readable or seekable.");
        }
    }
}

public class TableRepresentation
{
    public string Name { get; set; }

    public IEnumerable<ColumnModel> Columns { get; set; }

    public void AssignNameToTable(string name)
    {
        Name = name;
    }

    public void AddColumns(IEnumerable<ColumnModel> columns)
    {
        Columns = columns;
    }

    // public void Add
}

public class ColumnModel
{
}

public class FileDescription
{
    public string FileName { get; set; }

    public FileStream Stream { get; set; }
}
