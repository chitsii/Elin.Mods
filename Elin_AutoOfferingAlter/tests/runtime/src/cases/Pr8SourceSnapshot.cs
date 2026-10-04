#if RUNTIME_TEST
using System;
using System.Collections.Generic;
using System.Reflection;

// Definition fields only: never evaluate SourceRow.model/defaultRenderData/Color.linear or serialization callbacks.
public sealed class Pr8SourceSnapshot
{
    private readonly Ids stable = new Ids();
    private readonly Ids handles = new Ids();

    public string State(object value) { return Write(value, stable); }
    public string Values(object value) { return Write(value, new Ids()); }

    private string Write(object value, Ids ids)
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings {
            ContractResolver = new Fields(), ReferenceResolverProvider = () => new References(ids),
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
        };
        settings.Converters.Add(new Handles(handles));
        return Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.None, settings);
    }

    private sealed class Fields : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
        {
            var fields = new List<FieldInfo>();
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            fields.Sort((a, b) => StringComparer.Ordinal.Compare(a.DeclaringType.FullName + ":" + a.Name, b.DeclaringType.FullName + ":" + b.Name));
            var properties = new List<Newtonsoft.Json.Serialization.JsonProperty>();
            foreach (FieldInfo field in fields)
            {
                var property = base.CreateProperty(field, Newtonsoft.Json.MemberSerialization.Fields);
                property.PropertyName = field.DeclaringType.FullName + ":" + field.Name;
                // Newtonsoft suppresses reference metadata on non-writable collection properties.
                // Serialization only: no field is written and this helper has no deserializer.
                property.Readable = true; property.Writable = true; property.Ignored = false;
                // Do not invoke convention methods or apply native save exclusions to source definition fields.
                property.ShouldSerialize = null; property.GetIsSpecified = null;
                properties.Add(property);
            }
            return properties;
        }
        protected override Newtonsoft.Json.Serialization.JsonContract CreateContract(Type type)
        {
            var contract = base.CreateContract(type);
            // Boxing a Color array element creates a fresh object on each read; structs have value semantics.
            contract.IsReference = !type.IsValueType;
            contract.OnSerializingCallbacks.Clear(); contract.OnSerializedCallbacks.Clear();
            return contract;
        }
    }

    private sealed class Ids
    {
        private readonly Dictionary<object, string> values = new Dictionary<object, string>(Pr8ReferenceComparer<object>.Instance);
        public string Get(object value)
        {
            string id;
            if (!values.TryGetValue(value, out id)) { id = (values.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture); values.Add(value, id); }
            return id;
        }
    }
    private sealed class References : Newtonsoft.Json.Serialization.IReferenceResolver
    {
        private readonly Ids ids;
        private readonly HashSet<object> written = new HashSet<object>(Pr8ReferenceComparer<object>.Instance);
        public References(Ids source) { ids = source; }
        public string GetReference(object context, object value) { written.Add(value); return ids.Get(value); }
        public bool IsReferenced(object context, object value) { return written.Contains(value); }
        public void AddReference(object context, string reference, object value) { throw new NotSupportedException("Read-only source snapshot."); }
        public object ResolveReference(object context, string reference) { throw new NotSupportedException("Read-only source snapshot."); }
    }

    // Engine objects and cached model Cards are handles, not source-definition graphs/world traversal.
    // Material color is the relevant mutable native value seen in the failing path; capture its four components explicitly.
    private sealed class Handles : Newtonsoft.Json.JsonConverter
    {
        private readonly Ids ids;
        public Handles(Ids source) { ids = source; }
        public override bool CanRead => false;
        public override bool CanConvert(Type type)
        {
            // Game-defined ScriptableObjects (RenderData etc.) keep all their managed fields; engine internals are handles.
            return typeof(Card).IsAssignableFrom(type) || type == typeof(IntPtr) || type == typeof(UIntPtr)
                || (typeof(UnityEngine.Object).IsAssignableFrom(type) && type.Namespace != null && type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal));
        }
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (ReferenceEquals(value, null)) { writer.WriteNull(); return; }
            if (value is IntPtr) { writer.WriteValue(((IntPtr)value).ToInt64()); return; }
            if (value is UIntPtr) { writer.WriteValue(((UIntPtr)value).ToUInt64()); return; }
            writer.WriteStartObject(); writer.WritePropertyName("handleType"); writer.WriteValue(value.GetType().FullName);
            writer.WritePropertyName("reference"); writer.WriteValue(ids.Get(value));
            var material = value as UnityEngine.Material;
            if (!ReferenceEquals(material, null))
            {
                UnityEngine.Color color = material.color;
                writer.WritePropertyName("color"); writer.WriteStartObject();
                writer.WritePropertyName("r"); writer.WriteValue(color.r); writer.WritePropertyName("g"); writer.WriteValue(color.g);
                writer.WritePropertyName("b"); writer.WriteValue(color.b); writer.WritePropertyName("a"); writer.WriteValue(color.a);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type type, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        { throw new NotSupportedException("Read-only source snapshot."); }
    }
}
// RenderRow.GetSprite initializes this per-row cache on demand. Its contents need not
// equal another row's cache at a different usage stage; its geometry and isolation do.
public static class Pr8SourceSpriteCache
{
    public static bool IsField(FieldInfo field)
    {
        return field.DeclaringType == typeof(RenderRow) && field.Name == "sprites"
            && field.FieldType == typeof(UnityEngine.Sprite[,]) && field.IsNotSerialized;
    }
    public static void AssertIndependent(SourceThing.Row original, SourceThing.Row custom)
    {
        if (original.sprites != null && ReferenceEquals(original.sprites, custom.sprites))
            throw new InvalidOperationException("Shared native source sprites cache.");
        AssertShape(original); AssertShape(custom);
    }
    private static void AssertShape(SourceThing.Row row)
    {
        if (row.sprites == null) return;
        if (row._tiles == null || row.sprites.GetLength(0) != (row.skins == null ? 1 : row.skins.Length + 1)
            || row.sprites.GetLength(1) != (row._tiles.Length == 0 ? 1 : row._tiles.Length))
            throw new InvalidOperationException("Native sprites cache shape differs from row tiles/skins: " + row.id);
    }
}
#endif
