// Polyfill so `init` setters compile on netstandard2.0 with LangVersion=latest.
// The .NET 5+ BCL ships this type natively; we only need the stub here.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
