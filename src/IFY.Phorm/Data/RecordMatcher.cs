using System;

namespace IFY.Phorm.Data
{
    public interface IRecordMatcher
    {
        public abstract bool IsMatch(object parent, object child);
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
            => IsMatch(parent as TParent, child as TChild);
        public bool IsMatch(TParent? parent, TChild? child)
        {
            return parent != null && child != null && _matcher(parent, child);
        }
    }
}
