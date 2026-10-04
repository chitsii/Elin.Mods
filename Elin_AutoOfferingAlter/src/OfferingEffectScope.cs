#nullable disable
using System;

namespace Elin_AutoOfferingAlter
{
    // Only the top synchronous offer scope on this thread may redirect its exact owner Point.
    internal sealed class OfferingEffectScope<TPoint> : IDisposable where TPoint : class
    {
        [ThreadStatic] private static OfferingEffectScope<TPoint> current;
        private readonly OfferingEffectScope<TPoint> previous;
        private readonly TPoint ownerPoint;
        private readonly Func<bool> isCurrent;
        private readonly Func<TPoint> copyDestination;
        private bool disposed;

        internal OfferingEffectScope(TPoint ownerPoint, Func<bool> isCurrent, Func<TPoint> copyDestination)
        {
            this.ownerPoint = ownerPoint ?? throw new ArgumentNullException(nameof(ownerPoint));
            this.isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            this.copyDestination = copyDestination ?? throw new ArgumentNullException(nameof(copyDestination));
            previous = current;
            current = this;
        }

        internal static bool TryRedirect(ref TPoint from)
        {
            var scope = current;
            if (scope == null || !ReferenceEquals(from, scope.ownerPoint) || !scope.isCurrent()) return false;
            TPoint destination = scope.copyDestination();
            if (destination == null || ReferenceEquals(destination, scope.ownerPoint)) return false;
            from = destination;
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            if (!ReferenceEquals(current, this)) throw new InvalidOperationException("Offering FX scopes must unwind on their thread in reverse order.");
            current = previous;
            disposed = true;
        }
    }

    internal static class OfferingMapBounds
    {
        internal static bool Contains(Array cells, int x, int z)
        {
            return cells != null && cells.Rank == 2 && x >= cells.GetLowerBound(0) && z >= cells.GetLowerBound(1)
                && x <= cells.GetUpperBound(0) && z <= cells.GetUpperBound(1);
        }
    }
}
