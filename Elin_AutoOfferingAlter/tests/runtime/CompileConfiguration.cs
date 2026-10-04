#if RUNTIME_TEST
// Compile-only configuration. The csx builder supplies its real RuntimeV2Config.
public static class RuntimeV2Config
{
    public static readonly string ResultPath = "compile-only.json";
    public static readonly string RequiredNameContains = "RUNTIME_TEST";
    public static readonly string ModRoot = "compile-only";
    public static readonly string CaseIdFilter = "";
    public static readonly string TagFilter = "";
}
#endif
