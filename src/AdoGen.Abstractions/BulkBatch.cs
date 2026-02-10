using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public abstract class BulkBatch<T> where T : ISqlBulkModel
{
    protected readonly List<T> ItemsInternal;
    protected readonly List<BulkOp> OpsInternal;

    public int Count => ItemsInternal.Count;
    public IReadOnlyList<T> Items => ItemsInternal.AsReadOnly();
    public IReadOnlyList<BulkOp> Operations => OpsInternal.AsReadOnly();

    protected BulkBatch(int capacity = 0)
    {
        ItemsInternal = capacity > 0 ? new List<T>(capacity) : [];
        OpsInternal   = capacity > 0 ? new List<BulkOp>(capacity) : [];
    }

    public void Add(T item) => AddEntity(item, BulkOp.Insert);
    public void AddRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Insert);

    public void Update(T item) => AddEntity(item, BulkOp.Update);
    public void UpdateRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Update);

    public void Remove(T item) => AddEntity(item, BulkOp.Delete);
    public void RemoveRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Delete);

    private void AddEntity(T item, BulkOp operation)
    {
        ItemsInternal.Add(item);
        OpsInternal.Add(operation);
    }

    private void AddEntityRange(IEnumerable<T> items, BulkOp operation)
    {
        foreach (var item in items)
        {
            ItemsInternal.Add(item);
            OpsInternal.Add(operation);
        }
    }
    
    public abstract ValueTask<BulkApplyResult> SaveChangesAsync(
        SqlConnection connection, 
        CancellationToken ct,
        SqlTransaction transaction,
        int? commandTimeout = null);
    
    public void Clear()
    {
        ItemsInternal.Clear();
        OpsInternal.Clear();
    }
}