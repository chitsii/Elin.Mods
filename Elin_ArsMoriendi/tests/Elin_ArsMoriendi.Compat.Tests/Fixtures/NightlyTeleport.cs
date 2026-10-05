// Unmodified method from Elin-Decompiled 38a69bd9e976e46d5a512bb4811d586b96e70b3c:Elin/ActEffect.cs.
#nullable disable
public class ActEffect
{
	public static Point GetTeleportPos(Point org, int radius = 6, Chara target = null)
	{
		Point point = new Point();
		for (int i = 0; i < 10000; i++)
		{
			point.Set(org);
			point.x += EClass.rnd(radius) - EClass.rnd(radius);
			point.z += EClass.rnd(radius) - EClass.rnd(radius);
			if (point.IsValid && point.IsInBounds && !point.cell.blocked && point.Distance(org) >= radius / 3 + 1 - i / 50 && !point.cell.HasZoneStairs() && (target == null || i >= 100 || Los.IsVisible(point, target.pos)))
			{
				return point;
			}
		}
		return org.GetRandomNeighbor().GetNearestPoint();
	}
}
