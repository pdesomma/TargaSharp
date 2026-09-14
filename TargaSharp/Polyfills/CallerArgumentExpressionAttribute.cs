#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Tells the compiler to pass the source text of the named argument into this parameter; absent from .NET Framework.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the name of the parameter whose expression text is captured.
        /// </summary>
        /// <param name="parameterName">Name of the parameter whose argument expression is captured.</param>
        public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

        /// <summary>
        /// Gets the name of the parameter whose argument expression is captured.
        /// </summary>
        public string ParameterName { get; }
    }
}
#endif
