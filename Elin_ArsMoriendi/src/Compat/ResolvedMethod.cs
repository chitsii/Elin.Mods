using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal enum CompatResolutionMode
    {
        Unresolved = 0,
        Strict = 1,
        Fallback = 2,
    }

    internal readonly struct ResolvedMethod
    {
        public string SymbolId { get; }
        public MethodInfo? Method { get; }
        public string? ResolvedName { get; }
        public CompatResolutionMode ResolutionMode { get; }
        public string? MatchedSignature { get; }

        public bool IsResolved => Method != null;

        public bool IsStrictMatch => ResolutionMode == CompatResolutionMode.Strict;

        public ResolvedMethod(
            string symbolId,
            MethodInfo? method,
            string? resolvedName,
            CompatResolutionMode resolutionMode,
            string? matchedSignature)
        {
            SymbolId = symbolId;
            Method = method;
            ResolvedName = resolvedName;
            ResolutionMode = resolutionMode;
            MatchedSignature = matchedSignature;
        }
    }
}
