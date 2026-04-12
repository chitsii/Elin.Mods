using System.Collections.Generic;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsSpriteBudgetAllocatorTests
    {
        private readonly struct Candidate
        {
            public Candidate(int priority, float distance, string label)
            {
                Priority = priority;
                Distance = distance;
                Label = label;
            }

            public int Priority { get; }

            public float Distance { get; }

            public string Label { get; }
        }

        [Fact]
        public void ApplyBudget_PrefersHigherPriorityBeforeDistance()
        {
            List<Candidate> items = new List<Candidate>
            {
                new Candidate(2, 1f, "far-item"),
                new Candidate(0, 5f, "npc"),
                new Candidate(1, 0.5f, "installed"),
            };

            FpsSpriteBudgetAllocator.ApplyBudget(items, 2, static c => c.Priority, static c => c.Distance);

            Assert.Collection(
                items,
                item => Assert.Equal("npc", item.Label),
                item => Assert.Equal("installed", item.Label));
        }

        [Fact]
        public void ApplyBudget_PrefersNearerItemsWithinSamePriority()
        {
            List<Candidate> items = new List<Candidate>
            {
                new Candidate(1, 4f, "far"),
                new Candidate(1, 2f, "near"),
                new Candidate(1, 3f, "mid"),
            };

            FpsSpriteBudgetAllocator.ApplyBudget(items, 2, static c => c.Priority, static c => c.Distance);

            Assert.Collection(
                items,
                item => Assert.Equal("near", item.Label),
                item => Assert.Equal("mid", item.Label));
        }
    }
}
