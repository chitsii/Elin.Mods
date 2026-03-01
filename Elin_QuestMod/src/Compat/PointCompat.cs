using System;
using System.Reflection;

namespace Elin_QuestMod.Compat
{
    public static class PointCompat
    {
        private static MethodInfo _getNearestPointMethod;
        private static bool _getNearestPointChecked;

        // Uses vanilla allowInstalled=true default to avoid behavior drift.
        public static Point GetNearestPointCompat(
            this Point p,
            bool allowBlock,
            bool allowChara,
            bool allowInstalled = true,
            bool ignoreCenter = false)
        {
            if (!_getNearestPointChecked)
            {
                _getNearestPointMethod = typeof(Point).GetMethod(
                    "GetNearestPoint",
                    new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int) });

                if (_getNearestPointMethod == null)
                {
                    _getNearestPointMethod = typeof(Point).GetMethod(
                        "GetNearestPoint",
                        new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
                }

                _getNearestPointChecked = true;
            }

            if (_getNearestPointMethod == null)
            {
                ModLog.Error("Compatibility error: Point.GetNearestPoint method was not found.");
                return p;
            }

            try
            {
                var parameters = _getNearestPointMethod.GetParameters();
                object[] args;

                if (parameters.Length == 5)
                {
                    args = new object[] { allowBlock, allowChara, allowInstalled, ignoreCenter, 0 };
                }
                else
                {
                    args = new object[] { allowBlock, allowChara, allowInstalled, ignoreCenter };
                }

                return (Point)_getNearestPointMethod.Invoke(p, args);
            }
            catch (Exception ex)
            {
                ModLog.Warn("GetNearestPointCompat invoke failed: " + ex.Message);
                return p;
            }
        }
    }
}
