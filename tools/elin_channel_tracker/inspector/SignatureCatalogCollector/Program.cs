using System.Reflection;
using System.Text.Json;

namespace SignatureCatalogCollector;

internal sealed record TargetSpec(
    string TypeName,
    string CanonicalName,
    List<string> CandidateNames
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
        if (!options.TryGetValue("--targets", out var targetsPath) || string.IsNullOrWhiteSpace(targetsPath))
        {
            Console.Error.WriteLine("Missing required --targets path.");
            return 2;
        }
        if (!options.TryGetValue("--output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            Console.Error.WriteLine("Missing required --output path.");
            return 2;
        }

        var quiet = options.ContainsKey("--quiet");
        options.TryGetValue("--extra-dir", out var extraDir);

        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
            return 2;
        }
        if (!File.Exists(targetsPath))
        {
            Console.Error.WriteLine($"Targets file not found: {targetsPath}");
            return 2;
        }

        List<TargetSpec> targets;
        try
        {
            targets = LoadTargets(targetsPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load targets: {ex.Message}");
            return 2;
        }

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("No targets found.");
            return 2;
        }

        var assemblyFullPath = Path.GetFullPath(assemblyPath);
        var managedDir = Path.GetDirectoryName(assemblyFullPath) ?? ".";
        var resolverPaths = Directory.GetFiles(managedDir, "*.dll").ToList();
        if (!resolverPaths.Contains(assemblyFullPath, StringComparer.OrdinalIgnoreCase))
        {
            resolverPaths.Add(assemblyFullPath);
        }

        if (!string.IsNullOrWhiteSpace(extraDir) && Directory.Exists(extraDir))
        {
            resolverPaths.AddRange(Directory.GetFiles(extraDir, "*.dll"));
        }
        resolverPaths.Add(typeof(object).Assembly.Location);

        var resolver = new PathAssemblyResolver(resolverPaths.Distinct(StringComparer.OrdinalIgnoreCase));
        using var mlc = new MetadataLoadContext(resolver);

        var loadedAssemblies = new List<Assembly>();
        try
        {
            loadedAssemblies.Add(mlc.LoadFromAssemblyPath(assemblyFullPath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load assembly: {ex.Message}");
            return 2;
        }

        foreach (var dllPath in Directory.GetFiles(managedDir, "*.dll"))
        {
            if (string.Equals(dllPath, assemblyFullPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            TryLoadAssembly(mlc, dllPath, loadedAssemblies);
        }

        if (!string.IsNullOrWhiteSpace(extraDir) && Directory.Exists(extraDir))
        {
            foreach (var dllPath in Directory.GetFiles(extraDir, "*.dll"))
            {
                TryLoadAssembly(mlc, dllPath, loadedAssemblies);
            }
        }

        var allTypes = loadedAssemblies.SelectMany(SafeGetTypes).Where(t => t.FullName != null).ToList();
        var typesByFullName = allTypes
            .GroupBy(t => t.FullName!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var typesBySimpleName = allTypes
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var symbols = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target.TypeName))
            {
                continue;
            }

            var resolvedType = ResolveType(target.TypeName, typesByFullName, typesBySimpleName);
            if (resolvedType == null)
            {
                continue;
            }

            var candidateNames = target.CandidateNames.Count > 0
                ? target.CandidateNames
                : string.IsNullOrWhiteSpace(target.CanonicalName)
                    ? new List<string>()
                    : new List<string> { target.CanonicalName };

            foreach (var candidateName in candidateNames.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                var signatures = CollectSignatures(resolvedType, candidateName);
                if (signatures.Count > 0)
                {
                    symbols[$"{target.TypeName}.{candidateName}"] = signatures;
                }
            }
        }

        var outputDocument = new Dictionary<string, object?>
        {
            ["kind"] = "signature_catalog",
            ["schema_version"] = "1.0.0",
            ["generated_at_utc"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["symbols"] = symbols,
        };

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(outputDocument, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine
        );

        if (!quiet)
        {
            Console.WriteLine($"[SignatureCatalogCollector] targets={targets.Count} symbols={symbols.Count}");
            Console.WriteLine($"[SignatureCatalogCollector] output={outputPath}");
        }

        return 0;
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (arg == "--quiet")
            {
                result[arg] = "true";
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
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

    private static List<TargetSpec> LoadTargets(string targetsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(targetsPath));
        if (!document.RootElement.TryGetProperty("targets", out var targetsElement) || targetsElement.ValueKind != JsonValueKind.Array)
        {
            return new List<TargetSpec>();
        }

        var result = new List<TargetSpec>();
        foreach (var targetElement in targetsElement.EnumerateArray())
        {
            if (targetElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var typeName = GetString(targetElement, "type_name");
            var canonicalName = GetString(targetElement, "canonical_name");
            var candidateNames = GetStringArray(targetElement, "candidate_names");

            result.Add(new TargetSpec(typeName, canonicalName, candidateNames));
        }

        return result;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        var list = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(text);
                }
            }
        }
        return list;
    }

    private static void TryLoadAssembly(MetadataLoadContext mlc, string assemblyPath, List<Assembly> loadedAssemblies)
    {
        try
        {
            loadedAssemblies.Add(mlc.LoadFromAssemblyPath(assemblyPath));
        }
        catch
        {
            // Ignore native or incompatible DLLs.
        }
    }

    private static List<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null).Cast<Type>().ToList();
        }
    }

    private static Type? ResolveType(
        string typeName,
        Dictionary<string, Type> typesByFullName,
        Dictionary<string, List<Type>> typesBySimpleName)
    {
        if (typesByFullName.TryGetValue(typeName, out var full))
        {
            return full;
        }

        if (!typesBySimpleName.TryGetValue(typeName, out var simpleMatches))
        {
            return null;
        }

        return simpleMatches.Count == 1 ? simpleMatches[0] : null;
    }

    private static List<string> CollectSignatures(Type type, string candidateName)
    {
        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static;

        var signatures = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var method in type.GetMethods(flags).Where(m => m.Name == candidateName))
        {
            AddSignature(signatures, seen, FormatMethodSignature(method));
        }

        if (!candidateName.StartsWith("get_", StringComparison.Ordinal) &&
            !candidateName.StartsWith("set_", StringComparison.Ordinal))
        {
            foreach (var property in type.GetProperties(flags).Where(p => p.Name == candidateName))
            {
                AddSignature(signatures, seen, FormatTypeName(property.PropertyType));
            }
        }

        if (candidateName.StartsWith("get_", StringComparison.Ordinal))
        {
            var propertyName = candidateName[4..];
            foreach (var property in type.GetProperties(flags).Where(p => p.Name == propertyName))
            {
                AddSignature(signatures, seen, $"{FormatTypeName(property.PropertyType)}()");
            }
        }

        return signatures;
    }

    private static void AddSignature(List<string> signatures, HashSet<string> seen, string signature)
    {
        if (seen.Add(signature))
        {
            signatures.Add(signature);
        }
    }

    private static string FormatMethodSignature(MethodInfo method)
    {
        var returnType = FormatTypeName(method.ReturnType);
        var parameters = method.GetParameters()
            .Select(p => FormatTypeName(p.ParameterType))
            .ToArray();
        return $"{returnType}({string.Join(",", parameters)})";
    }

    private static string FormatTypeName(Type type)
    {
        if (type.IsByRef)
        {
            type = type.GetElementType() ?? type;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType == null ? "System.Array" : $"{FormatTypeName(elementType)}[]";
        }

        if (string.Equals(type.FullName, "System.Void", StringComparison.Ordinal))
        {
            return "void";
        }

        if (type.IsGenericType)
        {
            var genericBase = RemoveGenericArity(GetTypeDisplayName(type.GetGenericTypeDefinition()));
            var genericArgs = type.GetGenericArguments().Select(FormatTypeName);
            return $"{genericBase}<{string.Join(",", genericArgs)}>";
        }

        return RemoveGenericArity(GetTypeDisplayName(type));
    }

    private static string GetTypeDisplayName(Type type)
    {
        var fullName = type.FullName?.Replace('+', '.');
        var ns = type.Namespace ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(fullName) && ns.StartsWith("System", StringComparison.Ordinal))
        {
            return fullName;
        }

        return type.Name.Replace('+', '.');
    }

    private static string RemoveGenericArity(string typeName)
    {
        var marker = typeName.IndexOf('`');
        return marker >= 0 ? typeName[..marker] : typeName;
    }
}
