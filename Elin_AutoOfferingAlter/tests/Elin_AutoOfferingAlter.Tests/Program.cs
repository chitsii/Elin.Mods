using Elin_AutoOfferingAlter;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var tests = new (string Name, Action Run)[]
{
    ("rejected split batch is returned and processing stops", Tests.RejectedSplitBatchIsReturnedAndStops),
    ("water is not split and processing continues", Tests.WaterIsNotSplitAndContinues),
    ("exception returns split batch before rethrow", Tests.ExceptionReturnsSplitBatchBeforeRethrow),
    ("actor death stops before next batch", Tests.ActorDeathStopsBeforeNextBatch),
    ("pc change stops before next batch", Tests.PcChangeStopsBeforeNextBatch),
    ("faith change stops before next batch", Tests.FaithChangeStopsBeforeNextBatch),
    ("consumed batches preserve total quantity accounting", Tests.ConsumedBatchesPreserveQuantityAccounting),
    ("real metadata exposes declared non-generic SourceManager.Init only", Tests.RealMetadataExposesDeclaredSourceManagerInitOnly),
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static class Tests
{
    public static void RejectedSplitBatchIsReturnedAndStops()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item => { };

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Stopped, result, "result");
        AssertEqual(330, fixture.TotalQuantity, "total quantity");
        AssertEqual(1, fixture.OfferCalls, "offer calls");
        AssertEqual(1, fixture.Returned.Count, "returned split count");
    }

    public static void WaterIsNotSplitAndContinues()
    {
        var fixture = new Fixture(total: 12, unitValue: 0, nonConsuming: true);
        fixture.OfferAction = item => { };

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Completed, result, "result");
        AssertEqual(12, fixture.TotalQuantity, "total quantity");
        AssertEqual(1, fixture.OfferCalls, "offer calls");
        AssertEqual(0, fixture.SplitCalls, "split calls");
        AssertEqual(0, fixture.Returned.Count, "returned split count");
    }

    public static void ExceptionReturnsSplitBatchBeforeRethrow()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item => throw new InvalidOperationException("boom");

        AssertThrows<InvalidOperationException>(() => fixture.Run());
        AssertEqual(330, fixture.TotalQuantity, "total quantity");
        AssertEqual(1, fixture.Returned.Count, "returned split count");
    }

    public static void ActorDeathStopsBeforeNextBatch()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item =>
        {
            item.Destroyed = true;
            fixture.ActorAlive = false;
        };

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Stopped, result, "result");
        AssertEqual(300, fixture.TotalQuantity, "remaining quantity");
        AssertEqual(1, fixture.OfferCalls, "offer calls");
    }

    public static void PcChangeStopsBeforeNextBatch()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item =>
        {
            item.Destroyed = true;
            fixture.SamePc = false;
        };

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Stopped, result, "result");
        AssertEqual(300, fixture.TotalQuantity, "remaining quantity");
        AssertEqual(1, fixture.OfferCalls, "offer calls");
    }

    public static void FaithChangeStopsBeforeNextBatch()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item =>
        {
            item.Destroyed = true;
            fixture.SameFaith = false;
        };

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Stopped, result, "result");
        AssertEqual(300, fixture.TotalQuantity, "remaining quantity");
        AssertEqual(1, fixture.OfferCalls, "offer calls");
    }

    public static void ConsumedBatchesPreserveQuantityAccounting()
    {
        var fixture = new Fixture(total: 330, unitValue: 10);
        fixture.OfferAction = item => item.Destroyed = true;

        var result = fixture.Run();

        AssertEqual(OfferingBatchResult.Completed, result, "result");
        AssertEqual(0, fixture.TotalQuantity, "remaining quantity");
        AssertSequence(new[] { 30, 150, 150 }, fixture.OfferedQuantities, "offered quantities");
    }

    public static void RealMetadataExposesDeclaredSourceManagerInitOnly()
    {
        string elinDll = FindRepoFile("Elin_AutoOfferingAlter", "elin_link", "Elin_Data", "Managed", "Elin.dll");
        using FileStream stream = File.OpenRead(elinDll);
        using PEReader peReader = new(stream);
        MetadataReader reader = peReader.GetMetadataReader();

        TypeDefinition sourceManager = FindType(reader, "SourceManager");
        MethodDefinition sourceManagerInit = FindDeclaredMethod(reader, sourceManager, "Init");
        (bool sourceManagerInitIsGeneric, int sourceManagerInitParameterCount) = ReadMethodSignature(reader, sourceManagerInit);

        AssertEqual(false, sourceManagerInitIsGeneric, "SourceManager.Init generic");
        AssertEqual(0, sourceManagerInit.GetGenericParameters().Count, "SourceManager.Init generic parameter count");
        AssertEqual(0, sourceManagerInitParameterCount, "SourceManager.Init parameter count");

        TypeDefinition sourceThing = FindType(reader, "SourceThing");
        MethodDefinitionHandle? sourceThingInit = FindDeclaredMethodHandle(reader, sourceThing, "Init");
        if (sourceThingInit.HasValue)
        {
            throw new Exception("SourceThing unexpectedly declares Init; hook assumptions changed");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{name}: expected {expected}, got {actual}");
        }
    }

    private static void AssertSequence<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
    {
        if (expected.Count != actual.Count)
        {
            throw new Exception($"{name}: expected count {expected.Count}, got {actual.Count}");
        }
        for (int i = 0; i < expected.Count; i++)
        {
            AssertEqual(expected[i], actual[i], $"{name}[{i}]");
        }
    }

    private static void AssertThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new Exception($"expected {typeof(TException).Name}");
    }

    private static TypeDefinition FindType(MetadataReader reader, string name)
    {
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition definition = reader.GetTypeDefinition(handle);
            if (reader.GetString(definition.Name) == name)
            {
                return definition;
            }
        }

        throw new Exception($"metadata type not found: {name}");
    }

    private static MethodDefinition FindDeclaredMethod(MetadataReader reader, TypeDefinition type, string name)
    {
        MethodDefinitionHandle? handle = FindDeclaredMethodHandle(reader, type, name);
        if (!handle.HasValue)
        {
            throw new Exception($"metadata method not found: {name}");
        }

        return reader.GetMethodDefinition(handle.Value);
    }

    private static MethodDefinitionHandle? FindDeclaredMethodHandle(MetadataReader reader, TypeDefinition type, string name)
    {
        foreach (MethodDefinitionHandle handle in type.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            if (reader.GetString(method.Name) == name)
            {
                return handle;
            }
        }

        return null;
    }

    private static (bool IsGeneric, int ParameterCount) ReadMethodSignature(MetadataReader reader, MethodDefinition method)
    {
        BlobReader signature = reader.GetBlobReader(method.Signature);
        byte callingConvention = signature.ReadByte();
        bool isGeneric = (callingConvention & 0x10) != 0;
        if (isGeneric)
        {
            signature.ReadCompressedInteger();
        }

        int parameterCount = signature.ReadCompressedInteger();
        return (isGeneric, parameterCount);
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        foreach (string root in CandidateRoots())
        {
            DirectoryInfo? directory = new(root);
            while (directory != null)
            {
                string path = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(path))
                {
                    return path;
                }

                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeParts));
    }

    private static IEnumerable<string> CandidateRoots()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }
}

sealed class Fixture
{
    private readonly OfferingBatchRunner<FakeItem> runner;
    private readonly FakeItem root;

    public Fixture(int total, int unitValue, bool nonConsuming = false)
    {
        UnitValue = unitValue;
        NonConsuming = nonConsuming;
        root = new FakeItem(total, attached: true);
        runner = new OfferingBatchRunner<FakeItem>(
            getNum: item => item.Num,
            isDestroyed: item => item.Destroyed,
            isNonConsuming: _ => NonConsuming,
            split: Split,
            isDetached: item => !item.Attached,
            returnDetached: ReturnDetached,
            canContinue: () => ActorAlive && SamePc && SameFaith);
    }

    public int UnitValue { get; }
    public bool NonConsuming { get; }
    public bool ActorAlive { get; set; } = true;
    public bool SamePc { get; set; } = true;
    public bool SameFaith { get; set; } = true;
    public int OfferCalls { get; private set; }
    public int SplitCalls { get; private set; }
    public List<int> OfferedQuantities { get; } = new();
    public List<FakeItem> Returned { get; } = new();
    public Action<FakeItem> OfferAction { get; set; } = item => item.Destroyed = true;

    public int TotalQuantity => (root.Destroyed ? 0 : root.Num) + Returned.Where(item => !item.Destroyed).Sum(item => item.Num);

    public OfferingBatchResult Run()
    {
        return runner.Run(root, UnitValue, item =>
        {
            OfferCalls++;
            OfferedQuantities.Add(item.Num);
            OfferAction(item);
        });
    }

    private FakeItem Split(FakeItem item, int amount)
    {
        SplitCalls++;
        item.Num -= amount;
        return new FakeItem(amount, attached: false);
    }

    private void ReturnDetached(FakeItem item)
    {
        item.Attached = true;
        Returned.Add(item);
    }
}

sealed class FakeItem
{
    public FakeItem(int num, bool attached)
    {
        Num = num;
        Attached = attached;
    }

    public int Num { get; set; }
    public bool Attached { get; set; }
    public bool Destroyed { get; set; }
}
