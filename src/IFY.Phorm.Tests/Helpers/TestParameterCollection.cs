using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace IFY.Phorm.Tests
{
    internal class TestParameterCollection : IDataParameterCollection
    {
        private readonly List<IDbDataParameter> _parameters = new();

        public object this[string parameterName] { get => _parameters.First(p => p.ParameterName == parameterName); set => throw new NotImplementedException(); }
        public object? this[int index] { get => _parameters[index]; set => throw new NotImplementedException(); }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count => _parameters.Count;

        public bool IsSynchronized => false;

        public object SyncRoot { get; } = new();

        public int Add(object? value)
        {
            _parameters.Add((IDbDataParameter?)value ?? throw new NullReferenceException());
            return _parameters.Count - 1;
        }

        public void Clear()
        {
            _parameters.Clear();
        }

        public bool Contains(string parameterName)
        {
            return _parameters.Any(p => p.ParameterName == parameterName);
        }

        public bool Contains(object? value)
        {
            return _parameters.Any(p => p == (IDbDataParameter?)value);
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        public int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object? value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object? value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object? value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            _parameters.RemoveAt(index);
        }
    }
}
