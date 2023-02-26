namespace SinkingFeeling.ShipTarget
{
    /// <summary>
    /// Represents data stating how big the "target" looks, where it is based on where the
    /// drone a boat are.
    /// </summary>
    internal struct CameraViewTargetData
    {
        /// <summary>
        /// Where the target is visible.
        /// </summary>
        internal bool IsVisible;

        /// <summary>
        /// How tall the target looks in the camera.
        /// </summary>
        internal float HeightInRealWorld;

        /// <summary>
        /// How wide the target looks in the camera.
        /// </summary>
        internal float WidthInRealWorld;

        /// <summary>
        /// Angle of target from the drone.
        /// </summary>
        internal float AngleOfTargetInDegrees;

        /// <summary>
        /// Where the boat is located based on distanced, and AngleOfTargetInDegrees. 
        /// Although we can get it, this proves we have calculated the correct
        /// angle.
        /// </summary>
        internal PointF ShipLocated;

        /// <summary>
        /// Where the boat is relative to the camera.
        /// </summary>
        internal float PositionRelativeToCamera; // 0 left 1 = right
    }
}
