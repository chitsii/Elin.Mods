using System;

namespace Elin_ArsMoriendi
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class CompatibilityPatchAttribute : Attribute
    {
        public string SinceVersion { get; }
        public string Description { get; }

        public CompatibilityPatchAttribute(string sinceVersion, string description)
        {
            SinceVersion = sinceVersion;
            Description = description;
        }
    }
}
