using System.Collections.Generic;
using CwlQuestFramework;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// ArenaEnumMappingsをIEnumMappingProviderにアダプトするクラス
    /// </summary>
    internal class ArenaEnumMappingAdapter : IEnumMappingProvider
    {
        public bool TryGetMapping(string flagKey, out IDictionary<string, int> mapping)
        {
            if (ArenaEnumMappings.Mappings.TryGetValue(flagKey, out var dictMapping))
            {
                mapping = dictMapping;
                return true;
            }
            mapping = null;
            return false;
        }
    }
}
