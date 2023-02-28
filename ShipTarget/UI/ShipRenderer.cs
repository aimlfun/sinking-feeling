using SinkingFeelingPOC.Utils;

namespace SinkingFeelingPOC.ShipTarget.UI
{
    /// <summary>
    /// Class to render the Corvette Admiral Makarov.
    /// </summary>
    internal static class ShipRenderer
    {
        // points approximating the outline of the Makarov.
        readonly static PointF[] shipDefinition = new[] {
                        new PointF(4,21),
                        new PointF(1,15),
                        new PointF(24,15),
                        new PointF(38,0),
                        new PointF(42,6),
                        new PointF(45,5),
                        new PointF(47,6),
                        new PointF(47,10),
                        new PointF(56,7),
                        new PointF(59,10),
                        new PointF(64,6),
                        new PointF(66,10),
                        new PointF(67,8),
                        new PointF(69,9),
                        new PointF(73,14),
                        new PointF(76,14),
                        new PointF(97,16),
                        new PointF(97,19),
                        new PointF(96,21)
                        };

        private static readonly Bitmap fullsize;

        /// <summary>
        /// Constructor.
        /// </summary>
        static ShipRenderer()
        {
            fullsize = new("Assets/Ship Silhouette.png");

            // we are simulating night time, so we make it a negative image.
            fullsize = ImageUtils.CreateNegativeFromImage(fullsize);

            fullsize.MakeTransparent();
        }

        /// <summary>
        /// Draws the ship using points, filled.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="xCentreOfBoat"></param>
        /// <param name="width"></param>
        internal static void BasicRenderer(Graphics graphics, float xCentreOfBoat, float width)
        {
            // draw the ship
            PointF[] pointsScaledAndShifted = ScalePointsAdjustForCentre(xCentreOfBoat, width / 100);

            graphics.FillPolygon(Brushes.White, pointsScaledAndShifted);
            graphics.Flush();
        }

        /// <summary>
        /// The ship points need to be scaled as this isn't an image, but a polygon.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private static PointF[] ScalePointsAdjustForCentre(float x, float scale)
        {
            List<PointF> pointsScaled = new();

            foreach (PointF p in shipDefinition) pointsScaled.Add(new PointF(x + (p.X - 95f / 2f) * scale, 79 - (21 - p.Y) * scale));

            return pointsScaled.ToArray();
        }

        /// <summary>
        /// Draws the ship to the desired size. No kernel making it edges, this is the complete ship.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="xCentreOfBoat"></param>
        /// <param name="size"></param>
        internal static float RealisticRenderer(Graphics graphics, float xCentreOfBoat, float size)
        {
            xCentreOfBoat = (float)Math.Round(xCentreOfBoat);

            if (size > 15000) size = 15000;

            float heightScaledToSize = size / fullsize.Width * fullsize.Height;

            if (heightScaledToSize == 0) return 0;

            // make the image grey-scale (night-time look)
            Bitmap bitmapOfShipInGreyScale = ImageUtils.ToGrayScale(fullsize);

            // the image is full size, so shrink it to the required size
            ImageUtils.ResizeImage(ref bitmapOfShipInGreyScale, (int)size, (int)heightScaledToSize);
            bitmapOfShipInGreyScale.MakeTransparent(Color.White);
            
            graphics.DrawImageUnscaled(bitmapOfShipInGreyScale, (int)Math.Round(xCentreOfBoat - size / 2), 80 - bitmapOfShipInGreyScale.Height);
            graphics.Flush();

            // we don't use "using" because .ResizeImage() disposes of it
            bitmapOfShipInGreyScale.Dispose();

            return heightScaledToSize;
        }

        /// <summary>
        /// Draws the ship to the desired size but uses a kernel to convert image to edges only.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="xCentreOfBoat"></param>
        /// <param name="size"></param>
        internal static void EdgesOfRealisticRenderer(Graphics graphics, float xCentreOfBoat, float size)
        {
            float height = size / fullsize.Width * fullsize.Height;
            xCentreOfBoat = (float) Math.Round(xCentreOfBoat);

            // make the image grey-scale (night-time look)
            Bitmap bitmapOfShipInGreyScale = ImageUtils.ToGrayScale(fullsize);

            // the image is full size, so shrink it to the required size
            ImageUtils.ResizeImage(ref bitmapOfShipInGreyScale, (int)size, (int)height);
            bitmapOfShipInGreyScale.MakeTransparent();

            ImageUtils.LoadCurrentBitmap(bitmapOfShipInGreyScale);
            //ImageUtils.ConvertBitmapToGreyScale();

            // this applies "edge" filter
            bitmapOfShipInGreyScale = ImageUtils.ApplyRobertsFilter();

            graphics.DrawImageUnscaled(bitmapOfShipInGreyScale, (int)Math.Round(xCentreOfBoat - size / 2), 80 - bitmapOfShipInGreyScale.Height);
            graphics.Flush();
        }
    }
}

