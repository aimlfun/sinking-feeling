using SinkingFeeling.ShipTarget;
using SinkingFeeling.Utils;
using SinkingFeelingPOC.Map;
using SinkingFeelingPOC.ShipTarget.UI;
using SinkingFeelingPOC.Utils;
using SinkingFeelingPOC.World;
using System.Security.Cryptography;
using static SinkingFeelingPOC.DroneAttacking.ExplosiveDroneBoat;

namespace SinkingFeelingPOC.DroneAttacking.CameraView
{
    /// <summary>
    /// Returns what the drone "sees".
    /// This requires us to draw the ship when in view, enlarging as drone gets nearer.
    /// </summary>
    internal class Camera
    {
        /// <summary>
        /// Distance the drone camera sees.
        /// </summary>
        internal const float c_distanceToObject = 1000f; // 1KM

        /// <summary>
        /// Half the angle for field of view.
        /// </summary>
        internal const float c_HalfFOVAngleDegrees = 45;

        /// <summary>
        /// Waves taken from an image that was made negative, and gray scale.
        /// </summary>
        readonly Bitmap s_seaImage = new("Assets/MonochromeWaves.png");

        /// <summary>
        /// Cameras see a fixed amount of pixels, this is the width.
        /// </summary>
        internal const int CameraWidthPX = 200;

        /// <summary>
        /// Cameras see a fixed amount of pixels, this is the height.
        /// </summary>
        internal const int CameraHeightPX = 120;

        /// <summary>
        /// What the camera sees depends on where it is, and it's attached to a drone.
        /// </summary>
        private readonly ExplosiveDroneBoat droneCameraIsAttachedTo;

        /// <summary>
        /// The controller for everything. Needed to reference details about the world.
        /// </summary>
        private readonly WorldController worldController;

        /// <summary>
        /// This is the bitmap containing camera image used for the AI (200x80).
        /// </summary>
        Bitmap? CameraInputToAI = null;

        /// <summary>
        /// This is the bitmap containing the complete camera image (200x120).
        /// </summary>
        internal Bitmap? CameraImage = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="whatCameraIsAttachedTo"></param>
        internal Camera(ExplosiveDroneBoat whatCameraIsAttachedTo, WorldController controller)
        {
            droneCameraIsAttachedTo = whatCameraIsAttachedTo;
            worldController = controller;
        }

        /// <summary>
        /// Returns the input required by the AI.
        /// </summary>
        internal Bitmap? ImageFromCamera
        {
            get { return CameraInputToAI; }
        }

        /// <summary>
        /// Capture an image in the camera. To do this requires painting what the drone might see.
        /// </summary>
        /// <returns></returns>
        internal void TakeSnapShot()
        {
            Bitmap bitmapOfWhatTheCameraSees = new(CameraWidthPX, CameraHeightPX);

            using Graphics g = Graphics.FromImage(bitmapOfWhatTheCameraSees);
            g.Clear(Color.Black);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            int horizonVertical = 70 - 54 / 2 - 2;

            // we only provide an input if the ship is visible
            if (worldController.ShipIsVisible)
            {
                Bitmap imageOfShip = DrawShipToCameraBitmap(g);

                CameraInputToAI = imageOfShip;
            }
            else
                CameraInputToAI = null;

            // paint the waves underneath any ship
            AddWavesToTheCameraImage(bitmapOfWhatTheCameraSees, horizonVertical);

            // blur it a little 
            bitmapOfWhatTheCameraSees = ImageUtils.Convolve(bitmapOfWhatTheCameraSees, ImageUtils.GetGaussianBlurMatrix(3, 4));

            // overlay a [ + ] show centre and region ship needs to be in
            DrawCross(bitmapOfWhatTheCameraSees);

            CameraImage = DoublePixels(bitmapOfWhatTheCameraSees);
        }

        /// <summary>
        /// Draw the waves. We use a "sea" bitmap that we paint at random positions left, and random down to give a rough 
        /// sea moving look. It's not perfect, but our focus is on the use of AI, not seeing if we can simulate waves.
        /// </summary>
        /// <param name="bitmapOfWhatTheCameraSees"></param>
        /// <param name="horizonVertical"></param>
        private void AddWavesToTheCameraImage(Bitmap bitmapOfWhatTheCameraSees, int horizonVertical)
        {
            int dx = RandomNumberGenerator.GetInt32(0, 10);
            int dy = RandomNumberGenerator.GetInt32(0, 10);

            using Graphics graphic2 = Graphics.FromImage(bitmapOfWhatTheCameraSees);
            graphic2.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphic2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphic2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            graphic2.DrawImage(s_seaImage,
                            new Rectangle(0, horizonVertical, bitmapOfWhatTheCameraSees.Width, bitmapOfWhatTheCameraSees.Height - horizonVertical),
                            new Rectangle(dx, horizonVertical - dy, bitmapOfWhatTheCameraSees.Width + dx, bitmapOfWhatTheCameraSees.Height - horizonVertical - dy),
                            GraphicsUnit.Pixel);

            graphic2.Flush();
        }

        /// <summary>
        /// Draw the ship to scale on the camera.
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private Bitmap DrawShipToCameraBitmap(Graphics g)
        {
            if (droneCameraIsAttachedTo.Mode != DroneModes.targetLockon)
            {
                droneCameraIsAttachedTo.worldController.Telemetry(">> MODE: TARGET FOUND");
                droneCameraIsAttachedTo.Mode = DroneModes.targetLockon;
            }

            MapDefinition mapdef = worldController.mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn];

            CameraViewTargetData targetData = worldController.AcquireTargetData;

            // PositionRelativeToCamera is computed relative to triangle to determine where the ship is relative to camera
            int xOfShip = (int)Math.Round(targetData.PositionRelativeToCamera * (float) CameraWidthPX);

            // determine the size of the ship
            int size = (int)Math.Round(mapdef.XScaleToConvertMetresIntoPixels * targetData.WidthInRealWorld);

            // note the image used by the AI is 200x80, not 200x120
            Bitmap imageOfShip = new(CameraWidthPX, 80);

            // if too small, we don't render the ship
            if (size > 5)
            {
                using Graphics graphics = Graphics.FromImage(imageOfShip);
                float height = ShipRenderer.RealisticRenderer(graphics, xOfShip, size);

                g.DrawImageUnscaled(imageOfShip, 0, -38);
            }

            imageOfShip = ImageUtils.ToGrayScale(imageOfShip);

            return imageOfShip;
        }

        /// <summary>
        /// Creates an image twice as wide, and tall intentionally pixellating (4 pixels in place of 1).
        /// </summary>
        /// <param name="cameraImage"></param>
        /// <returns></returns>
        internal static Bitmap DoublePixels(Bitmap cameraImage)
        {
            ByteAccessibleBitmap b = new(cameraImage);
            ByteAccessibleBitmap pixellatedDoubleWidthAndHeightRepresentationOfCameraImage = new(new Bitmap(CameraWidthPX * 2, CameraHeightPX * 2));

            for (int y = 0; y < CameraHeightPX; y++)
            {
                for (int x = 0; x < CameraWidthPX; x++)
                {
                    Color c = b.GetARGBPixel(x, y);

                    pixellatedDoubleWidthAndHeightRepresentationOfCameraImage.SetARGBPixel(x * 2, y * 2, c);
                    pixellatedDoubleWidthAndHeightRepresentationOfCameraImage.SetARGBPixel(x * 2 + 1, y * 2, c);
                    pixellatedDoubleWidthAndHeightRepresentationOfCameraImage.SetARGBPixel(x * 2, y * 2 + 1, c);
                    pixellatedDoubleWidthAndHeightRepresentationOfCameraImage.SetARGBPixel(x * 2 + 1, y * 2 + 1, c);
                }
            }

            return pixellatedDoubleWidthAndHeightRepresentationOfCameraImage.UpdateBitmap();
        }

        /// <summary>
        /// Draws the [ + ] indicating centre and edges ship should be within.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="graphic"></param>
        private static void DrawCross(Bitmap bitmap)
        {
            using Graphics graphic = Graphics.FromImage(bitmap);

            // [
            graphic.DrawLine(Pens.White, bitmap.Width / 2 - 50, 28, bitmap.Width / 2 - 45, 28); // --
            graphic.DrawLine(Pens.White, bitmap.Width / 2 - 50, 28, bitmap.Width / 2 - 50, 43); // |
            graphic.DrawLine(Pens.White, bitmap.Width / 2 - 50, 43, bitmap.Width / 2 - 45, 43); // --

            // ]
            graphic.DrawLine(Pens.White, bitmap.Width / 2 + 50, 28, bitmap.Width / 2 + 45, 28); // --
            graphic.DrawLine(Pens.White, bitmap.Width / 2 + 50, 28, bitmap.Width / 2 + 50, 43); //  |
            graphic.DrawLine(Pens.White, bitmap.Width / 2 + 50, 43, bitmap.Width / 2 + 45, 43); // --

            // +
            graphic.DrawLine(Pens.White, bitmap.Width / 2 - 5, 35.5f, bitmap.Width / 2 + 5, 35.5f); // ---
            graphic.DrawLine(Pens.White, bitmap.Width / 2, 30.5f, bitmap.Width / 2, 40.5f);         //  |

            graphic.Flush();
        }
    }
}