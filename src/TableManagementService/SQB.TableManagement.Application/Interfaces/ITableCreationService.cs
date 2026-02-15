using SQB.Shared;

namespace SQB.TableManagement.Application.Interfaces;

public interface ITableCreationService
{
    // Task<Result> AddTableAsync(string name, string desc, string schema, IEnumerable<ColumnDdlModel> columns);

    Task<Result> CreateTableFromExistingAsync();

    Task<Result> RenameTableAsync(int tableId, string newName);
}
