// Unmodified method from Elin-Decompiled 35fac67c8cd3adbc145f37a55b0f04b1347dbb73:Elin/ActEffect.cs.
#nullable disable
public class ActEffect
{
	public static Point GetTeleportPos(Point org, int radius = 6)
	{
		Point point = new Point();
		for (int i = 0; i < 10000; i++)
		{
			point.Set(org);
			point.x += EClass.rnd(radius) - EClass.rnd(radius);
			point.z += EClass.rnd(radius) - EClass.rnd(radius);
			if (point.IsValid && point.IsInBounds && !point.cell.blocked && point.Distance(org) >= radius / 3 + 1 - i / 50 && !point.cell.HasZoneStairs())
			{
				return point;
			}
		}
		return org.GetRandomNeighbor().GetNearestPoint();
	}
}
