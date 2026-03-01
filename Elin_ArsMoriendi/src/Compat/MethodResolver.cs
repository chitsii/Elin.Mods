using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal static class MethodResolver
    {
        private static readonly Dictionary<string, ResolvedMethod> Cache = new(StringComparer.Ordinal);
        private static readonly object Sync = new();

        public static ResolvedMethod Resolve(CompatSymbol symbol)
        {
            lock (Sync)
            {
                if (Cache.TryGetValue(symbol.Id, out var cached))
                    return cached;

                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                    | (symbol.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

                var methods = symbol.OwnerType.GetMethods(bindingFlags);

                var resolved = ResolveStrict(symbol, methods)
                    ?? ResolveFallback(symbol, methods)
                    ?? new ResolvedMethod(
                        symbolId: symbol.Id,
                        method: null,
                        resolvedName: null,
                        resolutionMode: CompatResolutionMode.Unresolved,
                        matchedSignature: null);

                Cache[symbol.Id] = resolved;
                if (!resolved.IsResolved)
                {
                    ModLog.Warn($"Compat unresolved: {symbol.Id}");
                }
                else if (!resolved.IsStrictMatch)
                {
                    ModLog.Warn(
                        $"Compat fallback match: {symbol.Id} -> {resolved.ResolvedName} [{resolved.MatchedSignature}]");
                }
                return resolved;
            }
        }

        public static IReadOnlyList<ResolvedMethod> Warmup()
        {
            var results = new List<ResolvedMethod>(CompatSymbol.All.Count);
            foreach (var symbol in CompatSymbol.All)
                results.Add(Resolve(symbol));
            return results;
        }

        private static ResolvedMethod? ResolveStrict(
            CompatSymbol symbol,
            IEnumerable<MethodInfo> methods)
        {
            if (symbol.StrictSignatures.Count == 0)
                return null;

            foreach (var strict in symbol.StrictSignatures)
            {
                var candidate = methods.FirstOrDefault(strict.Matches);
                if (candidate == null)
                    continue;

                return new ResolvedMethod(
                    symbolId: symbol.Id,
                    method: candidate,
                    resolvedName: candidate.Name,
                    resolutionMode: CompatResolutionMode.Strict,
                    matchedSignature: strict.ToDisplayText());
            }

            return null;
        }

        private static ResolvedMethod? ResolveFallback(
            CompatSymbol symbol,
            IEnumerable<MethodInfo> methods)
        {
            foreach (var candidateName in symbol.CandidateNames)
            {
                var candidate = methods
                    .Where(m => m.Name == candidateName && symbol.Predicate(m))
                    .OrderByDescending(m => m.GetParameters().Length)
                    .FirstOrDefault();
                if (candidate == null)
                    continue;

                return new ResolvedMethod(
                    symbolId: symbol.Id,
                    method: candidate,
                    resolvedName: candidateName,
                    resolutionMode: CompatResolutionMode.Fallback,
                    matchedSignature: BuildSignatureText(candidate));
            }

            return null;
        }

        private static string BuildSignatureText(MethodInfo method)
        {
            var args = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
            return $"{method.ReturnType.Name} {method.Name}({args})";
        }
    }
}
