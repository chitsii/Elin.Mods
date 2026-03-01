using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal static class PatchTargetResolver
    {
        [CompatibilityPatch("EA23.184+", "ActPlan._Update/Update patch target compatibility.")]
        public static MethodBase? ResolveActPlanUpdate()
        {
            var resolved = MethodResolver.Resolve(CompatSymbol.ActPlanUpdate);
            if (!resolved.IsResolved || resolved.Method == null)
            {
                ModLog.Error("PatchTargetResolver: ActPlan._Update target was not resolved.");
                return null;
            }

            return resolved.Method;
        }
    }
}
