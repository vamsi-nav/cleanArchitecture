using System.Data;
using Dapper;

namespace CleanArchitecture.Infrastructure.Data;

/// <summary>Legacy reporting access retained from the pre-EF reporting module.</summary>
public class TodoReportingRepository
{
    private readonly IDbConnection _db;
    public TodoReportingRepository(IDbConnection db) => _db = db;

    // --- statically resolvable (7) ---
    public async Task<int> CountItems() =>
        await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.TodoItems");
    public async Task<object> ListLists() =>
        await _db.QueryAsync("SELECT Id, Title FROM dbo.TodoLists");
    public async Task CloseItem(int id) =>
        await _db.ExecuteAsync("UPDATE dbo.TodoItems SET Done = 1 WHERE Id = @id", new { id });
    public async Task PurgeAudit() =>
        await _db.ExecuteAsync("DELETE FROM dbo.TodoItemAudit");
    public async Task<object> AuditTrail() =>
        await _db.QueryAsync("SELECT * FROM dbo.TodoItemAudit");
    public async Task ArchiveList(int id) =>
        await _db.ExecuteAsync("INSERT INTO dbo.TodoListArchive SELECT * FROM dbo.TodoLists WHERE Id = @id", new { id });

    // --- runtime-assembled (5) ---
    public async Task<object> SortedItems(string sortColumn, string direction)
    {
        var sql = $"SELECT Id, Title FROM dbo.TodoItems ORDER BY {sortColumn} {direction}";
        return await _db.QueryAsync(sql);
    }
    public async Task<object> FilterByTenant(string suffix) =>
        await _db.QueryAsync($"SELECT * FROM TodoItems_{suffix}");
    public async Task<object> Search(string field, string term)
    {
        var sql = "SELECT * FROM dbo.TodoItems WHERE " + field + " LIKE '%" + term + "%'";
        return await _db.QueryAsync(sql);
    }
    public async Task BulkUpdate(string setClause) =>
        await _db.ExecuteAsync("UPDATE dbo.TodoLists SET " + setClause);
    public async Task<object> ReportFor(string period) =>
        await _db.QueryAsync($"SELECT * FROM Report_{period}_TodoItems");
}
