namespace SinkingFeelingPOC.Map
{
    /// <summary>
    /// A "map". This represents an image, and scale, providing methods to convert between PX and real coordinates.
    /// </summary>
    internal class MapDefinition
    {
        /// <summary>
        /// X scale used to convert map (metres) into pixels
        /// </summary>
        internal float XScaleToConvertMetresIntoPixels;

        /// <summary>
        /// Y scale used to convert map (metres) into pixels
        /// </summary>
        internal float YScaleToConvertMetresIntoPixels;

        /// <summary>
        /// The image of the map, used for rendering.
        /// </summary>
        internal Bitmap MapImage;

        /// <summary>
        /// Where the drone starts on the map in pixels.
        /// </summary>
        internal PointF DroneStartPointInPixels;

        /// <summary>
        /// Where the drone starts on the map in real world coordinates.
        /// This is converted from the PX coords, as we know those.
        /// </summary>
        internal PointF DroneStartPointInRealWorldCoordinates;

        /// <summary>
        /// Drone is artificially moved faster when zoomed out, so user doesn't have to wait
        /// 4 hours to cross the black sea. For zoomed in, it needs to go slower. The time
        /// multiplier is thus tracked against the map
        /// </summary>
        internal float TimeMultiplier = 15;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="xscalePXdivM"></param>
        /// <param name="yscalePXdivM"></param>
        /// <param name="startPointInPixels"></param>
        internal MapDefinition(string filePath, float xscalePXdivM, float yscalePXdivM, PointF startPointInPixels, float timeMultiplier)
        {
            Bitmap bitmapContainingMapImage = new(filePath);

            XScaleToConvertMetresIntoPixels = xscalePXdivM; // size in PX are based
            YScaleToConvertMetresIntoPixels = yscalePXdivM;

            MapImage = bitmapContainingMapImage;

            DroneStartPointInPixels = startPointInPixels;

            DroneStartPointInRealWorldCoordinates = ConvertPXToRealWorldCoordinates(startPointInPixels);
            TimeMultiplier = timeMultiplier;
        }

        /// <summary>
        /// Converts from pixels to real-world coordinates.
        /// </summary>
        /// <param name="startPointInPixels"></param>
        /// <returns></returns>
        internal PointF ConvertPXToRealWorldCoordinates(PointF startPointInPixels)
        {
            return new PointF(startPointInPixels.X / XScaleToConvertMetresIntoPixels, startPointInPixels.Y / YScaleToConvertMetresIntoPixels);
        }

        /// <summary>
        /// Converts from real-world coordinates to pixels.
        /// </summary>
        /// <param name="startPointInPixels"></param>
        /// <returns></returns>
        internal PointF ConvertRealworldCoordinatesToPX(PointF startPointInPixels)
        {
            return new PointF(startPointInPixels.X * XScaleToConvertMetresIntoPixels, startPointInPixels.Y * YScaleToConvertMetresIntoPixels);
        }
    }
}
