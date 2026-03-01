namespace Elin_ArsMoriendi
{
    internal static class PointCompat
    {
        [CompatibilityPatch("EA23.184", "Point.GetNearestPoint compatibility wrapper.")]
        public static Point GetNearestPointCompat(
            this Point point,
            bool allowBlock,
            bool allowChara,
            bool allowInstalled = true,
            bool ignoreCenter = false)
        {
            return SafeInvoke.GetNearestPoint(
                point,
                allowBlock,
                allowChara,
                allowInstalled,
                ignoreCenter);
        }
    }
}
