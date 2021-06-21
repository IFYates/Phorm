using System;

namespace IFY.Phorm.Data
{
    /// <summary>
    /// Mark a method as being the provider of a contract parameter.
    /// The method name will be parameter name and the return type is the value type sent.
    /// Note: Transformations are not supported on calculated values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CalculatedValueAttribute : Attribute, IContractMemberAttribute
    {
        public void SetContext(object? context) { }
    }
}