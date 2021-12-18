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

        public ResultsetAttribute(int order, string selectorPropertyName)
        {
            Order = order;
            SelectorPropertyName = selectorPropertyName;
        }

        public object[] FilterMatched(object parent, IEnumerable<object> children)
        {
            var selectorProp = parent.GetType().GetProperty(SelectorPropertyName);
            if (selectorProp?.PropertyType.IsAssignableTo(typeof(IRecordMatcher)) != true)
            {
                return Array.Empty<object>();
            }

            var selector = selectorProp.GetAccessors()[0].IsStatic
                ? (IRecordMatcher?)selectorProp.GetValue(null)
                : (IRecordMatcher?)selectorProp.GetValue(parent);
            return children.Where(c => selector?.IsMatch(parent, c) == true).ToArray();
        }
    }
}
