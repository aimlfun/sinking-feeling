namespace SinkingFeeling.AI
{
    /// <summary>
    /// Training data to teach AI where to hit the ship based on given pixels.
    /// </summary>
    internal class TrainingDataItemImageToTarget
    {
        /// <summary>
        /// Camera pixels for this image, used to train.
        /// </summary>
        internal double[] pixelsInAIinputForm = Array.Empty<double>();

        /// <summary>
        /// Target's position relative to image (centred on ship).
        /// </summary>
        internal float X;

        /// <summary>
        /// Size of the ship (to return)
        /// </summary>
        internal int Size;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="x"></param>
        /// <param name="size"></param>
        internal TrainingDataItemImageToTarget(double[] pixels, float x, int size)
        {
            pixelsInAIinputForm = pixels;
            X = x;
            Size = size;
        }
    }
}
