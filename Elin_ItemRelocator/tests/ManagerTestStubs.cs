// Minimal game boundaries for executing the production Manager without Unity or a game process.
namespace Newtonsoft.Json { internal sealed class NamespaceMarker { } }
namespace UnityEngine { internal sealed class NamespaceMarker { } }

public class Singleton<T> { }
public enum PlaceState { none, installed }
public class Trait { public bool CanBeDropped = true; }
public sealed class TraitToolBelt : Trait { }
public sealed class TraitAbility : Trait { }
public class Card {
    public Card parent;
    public ThingContainer things = new();
    public Card GetRootCard() => parent?.GetRootCard() ?? this;
}
public sealed class Thing : Card {
    public string id = "test";
    public string Name = "Test item";
    public int uid;
    public int Num = 1;
    public int c_equippedSlot;
    public bool isEquipped => c_equippedSlot != 0;
    public bool IsCursed;
    public bool isDestroyed, c_lockedHard, c_isImportant, IsContainer, isNPCProperty, isGifted;
    public bool IsIdentified = true;
    public int c_lockLv, ChildrenAndSelfWeight, SelfWeight, genLv;
    public PlaceState placeState;
    public Trait trait = new();
    public TestDna c_DNA;
    public TestElements elements = new();
    public List<Thing> AddedThings = new();
    public int GetPrice() => 0;
    public int GetValue() => 0;
    public void AddThing(Thing item) {
        AddedThings.Add(item);
        item.parent?.things.Remove(item);
        item.parent = this;
        things.Add(item);
    }
}
public sealed class ThingContainer : List<Thing> {
    public bool IsFull(Thing item) => false;
}
public sealed class Chara : Card {
    public bool IsPC;
    public bool IsAliveInCurrentZone = true;
    public TestBody body = new();
    public TestParty party = new();
    public void PlaySound(string sound) { }
}
public sealed class TestBody { public List<TestSlot> slots = new(); }
public sealed class TestSlot { public Thing thing; }
public sealed class TestParty { public List<Chara> members = new(); }
public sealed class TestMap { public List<Thing> things = new(); }
public sealed class TestPlayer { public TestHotbars hotbars = new(); }
public sealed class TestHotbars { public TestBar[] bars = Array.Empty<TestBar>(); }
public sealed class TestBar { public List<TestPage> pages = new(); }
public sealed class TestPage { public List<TestHotbarItem> items = new(); }
public sealed class TestHotbarItem { public Thing Thing; }
public sealed class TestDna { public int cost; }
public sealed class TestElements {
    public Dictionary<int, TestElement> dict = new();
    public int Value(int id) => 0;
}
public sealed class TestElement { public int Value; public TestElementSource source = new(); }
public sealed class TestElementSource { public int id; public string[] foodEffect; }
public sealed class TestSources { public TestElementSources elements = new(); }
public sealed class TestElementSources { public Dictionary<string, TestElementSource> alias = new(); }
public static class EClass {
    public static Chara pc;
    public static TestMap _map;
    public static TestPlayer player;
    public static TestSources sources = new();
}
public static class Game { public static string id = "test-game"; }
public static class Msg { public static void Say(string message) { } }

namespace Elin_ItemRelocator {
    public sealed class RelocationProfile {
        public string ContainerName;
        public int Version;
        public bool Enabled = true;
        public FilterScope Scope = FilterScope.Both;
        public ResultSortMode SortMode;
        public List<RelocationRule> Rules = new();
        public enum FilterScope { Inventory, Both, ZoneOnly, PetsOnly }
        public enum ResultSortMode {
            Default, PriceAsc, PriceDesc, EnchantMagAsc, EnchantMagDesc, TotalEnchantMagDesc,
            TotalWeightAsc, TotalWeightDesc, UnitWeightAsc, UnitWeightDesc, UidAsc, UidDesc,
            GenLvlAsc, GenLvlDesc, DnaAsc, DnaDesc, FoodPowerAsc, FoodPowerDesc,
            TotalFoodPowerAsc, TotalFoodPowerDesc
        }
    }
    public sealed class RelocationRule {
        public bool Enabled = true;
        public List<object> Conditions = new();
        public Func<Thing, bool> Match = _ => true;
        public bool IsMatch(Thing item) => Enabled && Match(item);
    }
    public sealed class PresetRepository {
        public void Save(string name, RelocationProfile profile) { }
        public RelocationProfile Load(string name) => new();
        public List<string> ListAll() => new();
        public void Rename(string oldName, string newName) { }
        public void Delete(string name) { }
    }
    public sealed class ConditionEnchantOr { public List<string> Runes = new(); }
    public sealed class ConditionFoodElement { public List<string> ElementIds = new(); }
    public static class ConditionRegistry {
        public static void ParseKeyOp(string text, out string key, out int op, out int value) {
            key = text;
            op = value = 0;
        }
    }
    public static class RelocatorLang {
        public enum LangKey { Msg_ContainerFull, Msg_RelocatedResult, Msg_NoMatchLog, Msg_Moved }
        public static string GetText(LangKey key) => key.ToString();
    }
}
