using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GameApiInspector;

internal record DependencyRecord(
    string DepType,
    string Target,
    string Risk,
    string Notes,
    string File,
    int Line,
    string Context,
    string? Signature
);

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = ParseArgs(args);
        if (!options.TryGetValue("--assembly", out var assemblyPath) || string.IsNullOrWhiteSpace(assemblyPath))
        {
            Console.Error.WriteLine("Missing required --assembly path.");
            return 2;
        }
        if (!options.TryGetValue("--deps", out var depsPath) || string.IsNullOrWhiteSpace(depsPath))
        {
            Console.Error.WriteLine("Missing required --deps path.");
            return 2;
        }

        var ciMode = options.ContainsKey("--ci");
        var strict = options.ContainsKey("--strict");
        var quiet = options.ContainsKey("--quiet");
        options.TryGetValue("--extra-dir", out var extraDir);

        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
            return 2;
        }
        if (!File.Exists(depsPath))
        {
            Console.Error.WriteLine($"Dependencies file not found: {depsPath}");
            return 2;
        }

        if (!quiet)
        {
            Console.WriteLine($"[GameApiInspector] Assembly: {assemblyPath}");
        }

        List<DependencyRecord> deps;
        try
        {
            var json = File.ReadAllText(depsPath);
            deps = JsonSerializer.Deserialize<List<DependencyRecord>>(json) ?? new List<DependencyRecord>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to read deps: {ex.Message}");
            return 2;
        }

        var managedDir = Path.GetDirectoryName(assemblyPath) ?? ".";
        var resolverPaths = Directory.GetFiles(managedDir, "*.dll").ToList();
        var extraDirs = new List<string>();
        if (!string.IsNullOrWhiteSpace(extraDir) && Directory.Exists(extraDir))
        {
            extraDirs.Add(extraDir);
            resolverPaths.AddRange(Directory.GetFiles(extraDir, "*.dll"));
        }
        resolverPaths.Add(typeof(object).Assembly.Location);

        var resolver = new PathAssemblyResolver(resolverPaths);
        using var mlc = new MetadataLoadContext(resolver);

        Assembly asm;
        try
        {
            asm = mlc.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load assembly: {ex.Message}");
            return 2;
        }

        var loadedAssemblies = new List<Assembly> { asm };

        foreach (var dll in Directory.GetFiles(managedDir, "*.dll"))
        {
            if (string.Equals(dll, assemblyPath, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                loadedAssemblies.Add(mlc.LoadFromAssemblyPath(dll));
            }
            catch
            {
                // ignore non-.NET assemblies
            }
        }

        foreach (var dir in extraDirs)
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                try
                {
                    loadedAssemblies.Add(mlc.LoadFromAssemblyPath(dll));
                }
                catch
                {
                    // ignore non-.NET assemblies
                }
            }
        }

        var allTypes = loadedAssemblies.SelectMany(SafeGetTypes).ToList();
        if (!quiet)
        {
            Console.WriteLine($"[GameApiInspector] Loaded assemblies: {loadedAssemblies.Count}");
            Console.WriteLine($"[GameApiInspector] Total types: {allTypes.Count}");
        }
        var hasCwlTypes = allTypes.Any(t => t.FullName != null && t.FullName.StartsWith("Cwl.", StringComparison.Ordinal));
        var typesByFullName = allTypes.Where(t => t.FullName != null)
            .GroupBy(t => t.FullName!)
            .ToDictionary(g => g.Key, g => g.First());
        var typesByName = allTypes
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        var errors = new List<string>();
        var warnings = new List<string>();
        var infos = new List<string>();

        foreach (var dep in deps)
        {
            var risk = dep.Risk ?? "Medium";
            var (typeName, memberName) = SplitTarget(dep.Target, dep.DepType);
            var type = ResolveType(typeName, typesByFullName, typesByName, out var typeWarning);

            if (typeWarning != null)
            {
                warnings.Add(FormatIssue(dep, $"Ambiguous type match for '{typeName}': {typeWarning}"));
                continue;
            }

            if (type == null)
            {
                var isCwlTarget = dep.DepType == "Reflection" && (
                    dep.Target.StartsWith("Cwl.", StringComparison.Ordinal) ||
                    dep.Target.StartsWith("CustomZone", StringComparison.Ordinal));

                if (isCwlTarget && !hasCwlTypes)
                {
                    infos.Add(FormatIssue(dep, $"CWL types not found (CWL not installed?): '{typeName}'"));
                }
                else
                {
                    AddIssue(dep, $"Type not found: '{typeName}'", risk, errors, warnings);
                }
                continue;
            }

            if (string.IsNullOrEmpty(memberName))
            {
                continue;
            }

            if (!HasMember(type, memberName!, dep.Signature))
            {
                AddIssue(dep, $"Member not found or signature mismatch: '{typeName}.{memberName}'", risk, errors, warnings);
            }
        }

        if (!quiet)
        {
            if (errors.Count > 0)
            {
                Console.WriteLine($"\n=== ERRORS ({errors.Count}) ===");
                foreach (var e in errors) Console.WriteLine(e);
            }
            if (warnings.Count > 0)
            {
                Console.WriteLine($"\n=== WARNINGS ({warnings.Count}) ===");
                foreach (var w in warnings) Console.WriteLine(w);
            }
            if (infos.Count > 0)
            {
                Console.WriteLine($"\n=== INFO ({infos.Count}) ===");
                foreach (var i in infos) Console.WriteLine(i);
            }
            if (errors.Count == 0 && warnings.Count == 0)
            {
                Console.WriteLine("Game API verification passed: No issues found");
            }
            Console.WriteLine($"\nSummary: {errors.Count} error(s), {warnings.Count} warning(s), {infos.Count} info(s)");
        }

        if (!ciMode)
        {
            return errors.Count > 0 ? 1 : 0;
        }

        if (errors.Count > 0) return 2;
        if (warnings.Count > 0 && strict) return 2;
        if (warnings.Count > 0) return 1;
        return 0;
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--"))
            {
                continue;
            }

            if (arg == "--ci" || arg == "--strict" || arg == "--quiet")
            {
                result[arg] = "true";
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                result[arg] = args[i + 1];
                i++;
            }
            else
            {
                result[arg] = "";
            }
        }
        return result;
    }

    private static List<Type> SafeGetTypes(Assembly asm)
    {
        try
        {
            return asm.GetTypes().ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).ToList()!;
        }
    }

    private static (string typeName, string? memberName) SplitTarget(string target, string depType)
    {
        if (string.IsNullOrWhiteSpace(target)) return ("", null);

        if (depType == "Direct")
        {
            var parts = target.Split('.');
            if (parts.Length >= 2)
            {
                return (parts[0], parts[1]);
            }
            return (parts[0], null);
        }

        if (target.Contains('.'))
        {
            // Try exact full name match first (type only)
            // If no type exists, we treat last segment as member
            var lastDot = target.LastIndexOf('.');
            if (lastDot > 0 && lastDot < target.Length - 1)
            {
                return (target.Substring(0, lastDot), target[(lastDot + 1)..]);
            }
        }

        return (target, null);
    }

    private static Type? ResolveType(
        string typeName,
        Dictionary<string, Type> typesByFullName,
        Dictionary<string, List<Type>> typesByName,
        out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        if (typesByFullName.TryGetValue(typeName, out var exact))
        {
            return exact;
        }

        if (typesByName.TryGetValue(typeName, out var matches))
        {
            if (matches.Count == 1) return matches[0];
            warning = string.Join(", ", matches.Select(t => t.FullName));
            return null;
        }

        return null;
    }

    private static bool HasMember(Type type, string memberName, string? signature)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        var methods = type.GetMethods(flags).Where(m => m.Name == memberName).ToList();
        if (methods.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return true;
            }

            var expected = ParseSignature(signature);
            foreach (var method in methods)
            {
                var actual = method.GetParameters()
                    .Select(p => NormalizeTypeName(p.ParameterType))
                    .ToList();
                if (SignaturesMatch(expected, actual))
                {
                    return true;
                }
            }
        }

        // Fields
        if (type.GetFields(flags).Any(f => f.Name == memberName))
        {
            return true;
        }

        // Properties
        if (type.GetProperties(flags).Any(p => p.Name == memberName))
        {
            return true;
        }

        if (memberName.StartsWith("get_", StringComparison.Ordinal))
        {
            var propName = memberName.Substring(4);
            if (type.GetProperties(flags).Any(p => p.Name == propName))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> ParseSignature(string signature)
    {
        return signature.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(NormalizeTypeName)
            .ToList();
    }

    private static bool SignaturesMatch(List<string> expected, List<string> actual)
    {
        if (expected.Count != actual.Count) return false;
        for (int i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    private static string NormalizeTypeName(Type type)
    {
        if (type.IsByRef)
        {
            type = type.GetElementType() ?? type;
        }

        return NormalizeTypeName(type.Name);
    }

    private static string NormalizeTypeName(string typeName)
    {
        return typeName switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "UInt16" => "ushort",
            "UInt32" => "uint",
            "UInt64" => "ulong",
            "Boolean" => "bool",
            "String" => "string",
            "Single" => "float",
            "Double" => "double",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Decimal" => "decimal",
            "Char" => "char",
            "Object" => "object",
            _ => typeName
        };
    }

    private static void AddIssue(
        DependencyRecord dep,
        string message,
        string risk,
        List<string> errors,
        List<string> warnings)
    {
        var lineInfo = $"{dep.File}:{dep.Line}";
        var detail = $"[{risk}] {lineInfo} {dep.DepType} {dep.Target} - {message}";
        if (risk.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(detail);
        }
        else
        {
            warnings.Add(detail);
        }
    }

    private static string FormatIssue(DependencyRecord dep, string message)
    {
        var lineInfo = $"{dep.File}:{dep.Line}";
        return $"[{dep.Risk}] {lineInfo} {dep.DepType} {dep.Target} - {message}";
    }
}
