#if !NET7_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class UnscopedRefAttribute : Attribute { }
}
#endif
