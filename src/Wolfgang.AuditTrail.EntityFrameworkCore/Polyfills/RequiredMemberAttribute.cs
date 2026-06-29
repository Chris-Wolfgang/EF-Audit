#if NET6_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill so the C# 11 <c>required</c> modifier compiles on net6.0.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute
{
}
#endif
