using System.Collections.Generic;

namespace AdoGen.Abstractions;

public sealed class BulkBatch<T> where T : ISqlBulkModel
{
    private readonly List<T> _items;
    private readonly List<BulkOp> _ops;
    public int Count => _items.Count;

    public IReadOnlyList<T> Items => _items;
    public IReadOnlyList<BulkOp> Operations => _ops;
    
    public BulkBatch(int capacity = 0)
    {
        _items = capacity > 0 ? new List<T>(capacity) : [];
        _ops = capacity > 0 ? new List<BulkOp>(capacity) : [];
    }
    
    public void Insert(T item) => Add(item, BulkOp.Insert);
    public void InsertRange(IEnumerable<T> items) => AddRange(items, BulkOp.Insert);

    public void Update(T item) => Add(item, BulkOp.Update);
    public void UpdateRange(IEnumerable<T> items) => AddRange(items, BulkOp.Update);

    public void Remove(T item) => Add(item, BulkOp.Delete);
    public void RemoveRange(IEnumerable<T> items) => AddRange(items, BulkOp.Delete);

    public void Add(T item, BulkOp operation)
    {
        _items.Add(item);
        _ops.Add(operation);
    }

    public void AddRange(IEnumerable<T> items, BulkOp operation)
    {
        foreach (var item in items)
        {
            _items.Add(item);
            _ops.Add(operation);
        }
    }
}