using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Data
{
    /// <summary>
    /// Mark a method as being the provider of a contract parameter.
    /// The method name will be parameter name and the return type is the value type sent.
    /// Note: Transformations are not supported on calculated values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
#if NETSTANDARD || NETCOREAPP
    [ExcludeFromCodeCoverage]
#else
    [ExcludeFromCodeCoverage(Justification = "No logic")]
#endif
    public class CalculatedValueAttribute : Attribute, IContractMemberAttribute
    {
        public void SetContext(object? context) { }
    }
}