// Deliberately unsupported future signature: must fail before selecting any position.
public class ActEffect
{
    public static Point GetTeleportPos(Point org, int radius, bool differentMeaning = false)
        => throw new System.InvalidOperationException("An unknown signature must never be invoked.");
}
