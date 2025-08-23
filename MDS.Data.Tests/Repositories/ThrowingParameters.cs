using System.Data.Common;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingParameters : DbParameterCollection
{
    readonly List<object> _items = new();
    public override int Add(object value) { _items.Add(value); return _items.Count - 1; }
    public override void AddRange(Array values) { foreach (var v in values) _items.Add(v!); }
    public override void Clear() => _items.Clear();
    public override bool Contains(object value) => _items.Contains(value);
    public override bool Contains(string value) => false;
    public override void CopyTo(Array array, int index) { }
    public override int Count => _items.Count;
    public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
    protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
    protected override DbParameter GetParameter(string parameterName) => null!;
    public override int IndexOf(object value) => _items.IndexOf(value);
    public override int IndexOf(string parameterName) => -1;
    public override void Insert(int index, object value) => _items.Insert(index, value);
    public override bool IsFixedSize => false;
    public override bool IsReadOnly => false;
    public override bool IsSynchronized => false;
    public override void Remove(object value) => _items.Remove(value);
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName) { }
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) { }
    public override object SyncRoot => this;
}