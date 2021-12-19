using System;
using System.Collections.Generic;
using System.Linq;

namespace IFY.Phorm.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ResultsetAttribute : Attribute
    {
        public int Order { get; }
        public string SelectorPropertyName { get; }

        private Type? _lastSelectorType = null;
        private IRecordMatcher? _lastMatcher = null;

        public ResultsetAttribute(int order, string selectorPropertyName)
        {
            Order = order;
            SelectorPropertyName = selectorPropertyName;
        }

        public object[] FilterMatched(object parent, IEnumerable<object> children)
        {
            if (_lastSelectorType != parent.GetType())
            {
                var selectorProp = parent.GetType().GetProperty(SelectorPropertyName);
                if (selectorProp?.PropertyType.IsAssignableTo(typeof(IRecordMatcher)) != true)
                {
                    throw new InvalidCastException($"Selector property '{SelectorPropertyName}' does not return IRecordMatcher.");
                }

                _lastSelectorType = parent.GetType();
                _lastMatcher = selectorProp.GetAccessors()[0].IsStatic
                    ? (IRecordMatcher?)selectorProp.GetValue(null)
                    : (IRecordMatcher?)selectorProp.GetValue(parent);
            }
            return children.Where(c => _lastMatcher!.IsMatch(parent, c) == true).ToArray();
        }
    }
}
