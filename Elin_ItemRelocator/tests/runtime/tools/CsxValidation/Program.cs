using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

Pr9DestructionGuardChecks.Verify();

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: CsxValidation generated.csx game-directory ItemRelocator.dll");
    return 2;
}

// Read metadata and emit in memory only. Never load or evaluate the script.
var paths = Directory.GetFiles(Path.Combine(args[1], "Elin_Data", "Managed"), "*.dll")
    .Concat(Directory.GetFiles(Path.Combine(args[1], "BepInEx", "core"), "*.dll"))
    .Append(Path.GetFullPath(args[2]));
var references = new List<MetadataReference>();
foreach (var path in paths)
{
    try { references.Add(MetadataReference.CreateFromFile(path)); }
    catch (BadImageFormatException) { }
}
var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(args[0]),
    new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script), args[0]);
var compilation = CSharpCompilation.CreateScriptCompilation("Pr9OfflineValidation", tree, references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
using var output = new MemoryStream();
var result = compilation.Emit(output);
foreach (var group in result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).GroupBy(d => d.ToString()))
    Console.WriteLine(group.Key + " [occurrences=" + group.Count() + "]");
Console.WriteLine("script-emit=" + result.Success + "; game-script-executed=false; references=" + references.Count);
return result.Success ? 0 : 1;
