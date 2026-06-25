#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

// S2094: empty class is REQUIRED — IsExternalInit is the well-known marker
// type the C# compiler looks up to enable `init`-only setters on TFMs whose
// reference assemblies don't ship it. Replacing with an interface or adding
// members breaks the compiler contract.
#pragma warning disable S2094
internal static class IsExternalInit
{
}
#pragma warning restore S2094
#endif
