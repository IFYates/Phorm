﻿using System;

namespace IFY.Phorm.Data
{
    public interface IRecordMatcher
    {
        bool IsMatch(object parent, object child);
    }

    public class RecordMatcher<TParent, TChild> : IRecordMatcher
        where TParent : class
        where TChild : class
    {
        private readonly Func<TParent, TChild, bool> _matcher;

        public RecordMatcher(Func<TParent, TChild, bool> matcher)
        {
            _matcher = matcher;
        }

        bool IRecordMatcher.IsMatch(object parent, object child)
        {
            var typedParent = parent as TParent ?? throw new InvalidCastException($"Parent entity type '{parent.GetType().FullName}' could not be used for matcher expecting type '{typeof(TParent).FullName}'.");
            var typedChild = child as TChild ?? throw new InvalidCastException($"Child entity type '{child.GetType().FullName}' could not be used for matcher expecting type '{typeof(TChild).FullName}'.");
            return IsMatch(typedParent, typedChild);
        }
        public bool IsMatch(TParent parent, TChild child)
        {
            return _matcher(parent, child);
        }
    }
}