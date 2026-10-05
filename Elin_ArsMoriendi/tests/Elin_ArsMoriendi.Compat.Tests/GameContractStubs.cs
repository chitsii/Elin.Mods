using System;
using System.Collections.Generic;

// Controlled map/random/LOS boundary only; GetTeleportPos itself is source-derived.
public class Point
{
    public int x, z;
    public bool IsValid => true;
    public bool IsInBounds => true;
    public Cell cell => new Cell();
    public void Set(Point origin) { x = origin.x; z = origin.z; }
    public int Distance(Point origin) => Math.Max(Math.Abs(x - origin.x), Math.Abs(z - origin.z));
    public Point GetRandomNeighbor() => this;
    public Point GetNearestPoint() => ContractWorld.Fallback;
}

public class Cell
{
    public bool blocked => ContractWorld.Blocked;
    public bool HasZoneStairs() => false;
}

public class Chara { public Point pos = new Point(); }
public class Card { }
public class Thing { }
public class Element { }
public class Quest { }
public class ActPlan { }
public class PointTarget { }
public enum AttackSource { None }

public static class ContractWorld
{
    public static bool Blocked;
    public static int RandomCalls, LosCalls;
    public static readonly Point Fallback = new Point { x = -999, z = -999 };
    public static Exception? RandomFailure;
    public static void Reset()
    {
        Blocked = false;
        RandomCalls = LosCalls = 0;
        RandomFailure = null;
    }
}

public class EClass
{
    public static int rnd(int radius)
    {
        if (ContractWorld.RandomFailure != null) throw ContractWorld.RandomFailure;
        // x displacement radius - 1, z displacement 0; detects dropped/swapped radius.
        return ContractWorld.RandomCalls++ % 4 == 0 ? radius - 1 : 0;
    }
}

public static class Los
{
    public static bool IsVisible(Point from, Point to) { ContractWorld.LosCalls++; return false; }
}

namespace Elin_ArsMoriendi
{
    internal static class ModLog
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void Warn(string message) => Warnings.Add(message);
    }
}
