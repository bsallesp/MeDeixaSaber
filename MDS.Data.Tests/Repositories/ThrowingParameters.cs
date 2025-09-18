using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingParameters : DbParameterCollection
{
    private readonly List<object> _list = [];
    public override int Count => _list.Count;
    public override object SyncRoot => _list;

    public override int Add(object value)
    {
        _list.Add(value);
        return _list.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            _list.Add(value);
        }
    }

    public override void Clear() => _list.Clear();
    public override bool Contains(string value) => false;
    public override bool Contains(object value) => _list.Contains(value);
    public override void CopyTo(Array array, int index) => throw new NotSupportedException();
    public override IEnumerator GetEnumerator() => _list.GetEnumerator();
    public override int IndexOf(string parameterName) => -1;
    public override int IndexOf(object value) => _list.IndexOf(value);
    public override void Insert(int index, object value) => _list.Insert(index, value);
    public override void Remove(object value) => _list.Remove(value);
    public override void RemoveAt(string parameterName) => throw new NotSupportedException();
    public override void RemoveAt(int index) => _list.RemoveAt(index);
    protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
    protected override DbParameter GetParameter(int index) => (DbParameter)_list[index];
    protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
}