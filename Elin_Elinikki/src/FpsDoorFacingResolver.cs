namespace Elin_Elinikki
{
    internal static class FpsDoorFacingResolver
    {
        public static int ResolveFaceDir(int blockDir, bool isOpen)
        {
            int normalizedBlockDir = ((blockDir % 4) + 4) % 4;
            int closedFaceDir = normalizedBlockDir == 1 ? 1 : 0;
            if (!isOpen)
            {
                return closedFaceDir;
            }

            return closedFaceDir == 0 ? 1 : 0;
        }
    }
}
