namespace Elin_Elinikki
{
    internal struct FpsViewState
    {
        public static readonly FpsViewState Default = new FpsViewState
        {
            HasCustomYaw = false,
            YawRadians = 0f,
            PitchOffset = 0f,
            CameraDistance = 0f,
            CameraHeightOffset = 0f
        };

        public bool HasCustomYaw;
        public float YawRadians;
        public float PitchOffset;
        public float CameraDistance;
        public float CameraHeightOffset;
    }
}
