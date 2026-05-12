#if NET6_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Polyfill so consumers can mark constructors that initialize <c>required</c>
/// members on net6.0.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
internal sealed class SetsRequiredMembersAttribute : Attribute
{
}
#endif
