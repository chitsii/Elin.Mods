using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomModEntryDiscoveryTests : IDisposable
{
    private readonly string _root;
    private readonly string _modsRoot;
    private readonly string _configRoot;

    public DoomModEntryDiscoveryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "JustDoomItTests", Guid.NewGuid().ToString("N"));
        _modsRoot = Path.Combine(_root, "wad", "mods");
        _configRoot = Path.Combine(_root, "profiles", "mod_entry_configs");
        Directory.CreateDirectory(_modsRoot);
        Directory.CreateDirectory(_configRoot);
    }

    [Fact]
    public void DiscoverEntries_SingleWadFolder_IsReadySingle()
    {
        var folder = Path.Combine(_modsRoot, "neis");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "NEIS.wad"), new byte[] { 1, 2, 3 });

        var entries = DoomModEntryCore.DiscoverEntries(_modsRoot, _configRoot);
        var entry = Assert.Single(entries);

        Assert.Equal("neis", entry.EntryId);
        Assert.Equal(DoomModEntryState.ReadySingle, entry.State);
        Assert.Equal("NEIS.wad", Assert.Single(entry.DetectedWadFiles));
        Assert.Equal("NEIS.wad", Assert.Single(entry.LaunchWadFiles));
    }

    [Fact]
    public void DiscoverEntries_MultiWadWithoutConfig_IsSetupNeeded()
    {
        var folder = Path.Combine(_modsRoot, "alien_vendetta");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "AV.WAD"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(folder, "AVMOVFIX.WAD"), new byte[] { 2 });

        var entries = DoomModEntryCore.DiscoverEntries(_modsRoot, _configRoot);
        var entry = Assert.Single(entries);

        Assert.Equal(DoomModEntryState.SetupNeeded, entry.State);
        Assert.Empty(entry.LaunchWadFiles);
    }

    [Fact]
    public void DiscoverEntries_MultiWadWithConfig_IsReadyMulti()
    {
        var folder = Path.Combine(_modsRoot, "alien_vendetta");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "AV.WAD"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(folder, "AVMOVFIX.WAD"), new byte[] { 2 });

        DoomModEntryCore.SaveEntryConfig(_configRoot, "alien_vendetta", new DoomModEntryConfig
        {
            display_name = "Alien Vendetta",
            main_wad_file = "AV.WAD",
            wad_order = new[] { "AV.WAD", "AVMOVFIX.WAD" }
        });

        var entries = DoomModEntryCore.DiscoverEntries(_modsRoot, _configRoot);
        var entry = Assert.Single(entries);

        Assert.Equal(DoomModEntryState.ReadyMulti, entry.State);
        Assert.Equal(new[] { "AV.WAD", "AVMOVFIX.WAD" }, entry.LaunchWadFiles);
        Assert.Equal("Alien Vendetta", entry.DisplayName);
        Assert.Equal("AV.WAD", entry.MainWadFile);
    }

    [Fact]
    public void DiscoverEntries_WrapperFolderRescue_FindsChildContentRoot()
    {
        var folder = Path.Combine(_modsRoot, "wrapped_mod");
        var child = Path.Combine(folder, "data");
        Directory.CreateDirectory(child);
        File.WriteAllText(Path.Combine(folder, "README.txt"), "info");
        File.WriteAllBytes(Path.Combine(child, "MOD.WAD"), new byte[] { 1 });

        var entries = DoomModEntryCore.DiscoverEntries(_modsRoot, _configRoot);
        var entry = Assert.Single(entries);

        Assert.Equal(DoomModEntryState.ReadySingle, entry.State);
        Assert.Equal(child, entry.ContentRootPath);
    }

    [Fact]
    public void DiscoverEntries_UnsupportedSidecar_IsErrorUnsupported()
    {
        var folder = Path.Combine(_modsRoot, "unsupported_mod");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "MAPS.WAD"), new byte[] { 1 });
        File.WriteAllText(Path.Combine(folder, "PATCH.DEH"), "boom");

        var entries = DoomModEntryCore.DiscoverEntries(_modsRoot, _configRoot);
        var entry = Assert.Single(entries);

        Assert.Equal(DoomModEntryState.ErrorUnsupported, entry.State);
    }

    [Fact]
    public void ResetEntryConfig_DeletesSavedConfig()
    {
        DoomModEntryCore.SaveEntryConfig(_configRoot, "alien_vendetta", new DoomModEntryConfig
        {
            main_wad_file = "AV.WAD",
            wad_order = new[] { "AV.WAD", "AVMOVFIX.WAD" }
        });

        Assert.True(File.Exists(Path.Combine(_configRoot, "alien_vendetta.json")));

        DoomModEntryCore.ResetEntryConfig(_configRoot, "alien_vendetta");

        Assert.False(File.Exists(Path.Combine(_configRoot, "alien_vendetta.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}

