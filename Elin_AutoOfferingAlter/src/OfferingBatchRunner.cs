using System;

namespace Elin_AutoOfferingAlter
{
    public enum OfferingBatchResult
    {
        Completed,
        Stopped
    }

    public sealed class OfferingBatchRunner<TItem>
    {
        private readonly Func<TItem, int> getNum;
        private readonly Func<TItem, bool> isDestroyed;
        private readonly Func<TItem, bool> isNonConsuming;
        private readonly Func<TItem, int, TItem> split;
        private readonly Func<TItem, bool> isDetached;
        private readonly Action<TItem> returnDetached;
        private readonly Func<bool> canContinue;

        public OfferingBatchRunner(
            Func<TItem, int> getNum,
            Func<TItem, bool> isDestroyed,
            Func<TItem, bool> isNonConsuming,
            Func<TItem, int, TItem> split,
            Func<TItem, bool> isDetached,
            Action<TItem> returnDetached,
            Func<bool> canContinue)
        {
            this.getNum = getNum;
            this.isDestroyed = isDestroyed;
            this.isNonConsuming = isNonConsuming;
            this.split = split;
            this.isDetached = isDetached;
            this.returnDetached = returnDetached;
            this.canContinue = canContinue;
        }

        public OfferingBatchResult Run(TItem item, int unitValue, Action<TItem> offer)
        {
            if (!canContinue())
            {
                return OfferingBatchResult.Stopped;
            }

            if (isNonConsuming(item))
            {
                return OfferOne(item, offer, allowNonConsumed: true);
            }

            int num = getNum(item);
            if (num <= 1 || unitValue <= 0)
            {
                return OfferOne(item, offer, allowNonConsumed: false);
            }

            int batchSize = CalculateBatchSize(unitValue);
            int remainder = num % batchSize;

            if (remainder > 0)
            {
                OfferingBatchResult result = OfferAmount(item, remainder, offer);
                if (result == OfferingBatchResult.Stopped)
                {
                    return result;
                }
            }

            while (!IsConsumed(item) && getNum(item) > 0)
            {
                int current = getNum(item);
                int amount = current > batchSize ? batchSize : current;
                OfferingBatchResult result = OfferAmount(item, amount, offer);
                if (result == OfferingBatchResult.Stopped)
                {
                    return result;
                }
            }

            return OfferingBatchResult.Completed;
        }

        public static int CalculateBatchSize(int unitValue)
        {
            const int targetValue = 1500;
            const int maxValue = 3000;

            int batchSize = (targetValue + unitValue - 1) / unitValue;
            if (batchSize * unitValue > maxValue)
            {
                batchSize = Math.Max(1, maxValue / unitValue);
            }
            return Math.Max(1, batchSize);
        }

        private OfferingBatchResult OfferAmount(TItem item, int amount, Action<TItem> offer)
        {
            if (!canContinue())
            {
                return OfferingBatchResult.Stopped;
            }

            TItem batch = amount < getNum(item) ? split(item, amount) : item;
            return OfferOne(batch, offer, allowNonConsumed: false);
        }

        private OfferingBatchResult OfferOne(TItem item, Action<TItem> offer, bool allowNonConsumed)
        {
            try
            {
                offer(item);
            }
            catch
            {
                ReturnIfDetachedAndAlive(item);
                throw;
            }

            if (!canContinue())
            {
                ReturnIfDetachedAndAlive(item);
                return OfferingBatchResult.Stopped;
            }

            if (!IsConsumed(item))
            {
                ReturnIfDetachedAndAlive(item);
                return allowNonConsumed ? OfferingBatchResult.Completed : OfferingBatchResult.Stopped;
            }

            return OfferingBatchResult.Completed;
        }

        private bool IsConsumed(TItem item)
        {
            return isDestroyed(item) || getNum(item) <= 0;
        }

        private void ReturnIfDetachedAndAlive(TItem item)
        {
            if (!IsConsumed(item) && isDetached(item))
            {
                returnDetached(item);
            }
        }
    }
}

