using SinkingFeeling.AI;
using SinkingFeelingPOC.DroneAttacking.CameraView;
using SinkingFeelingPOC.Utils;

namespace SinkingFeelingPOC.DroneAttacking.NavigationMethods
{
    /// <summary>
    /// Provides navigation of boat via AI (intentionally crashing into the Makarov).
    /// </summary>
    internal class AINavigation : NavigationBase
    {
        /// <summary>
        /// This is the object that acts as the brain
        /// </summary>
        readonly CameraToTargetBrain brain;

        /// <summary>
        /// This is where we last found the target with regards to left edge of camera.
        /// </summary>
        internal int locationCentreOfTargetWithRespectToCamera;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="whatCameraIsAttachedTo"></param>
        internal AINavigation(ExplosiveDroneBoat whatCameraIsAttachedTo) : base(whatCameraIsAttachedTo)
        {
            brain = new(whatCameraIsAttachedTo.worldController);
        }

        /// <summary>
        /// This is the last angle the drone was heading, used to detect bad AI predictions (large deviation). 
        /// </summary>
        float lastAngleToTurn = 360;

        /// <summary>
        /// true - we are heading within <10 degrees of the centre of the Makarov.
        /// </summary>
        bool hasLocked = false;

        /// <summary>
        /// This is the angle the drone needs to head. It is stored in case it is required to substitute bad
        /// AI predictions. It is better to head in the same direction (knowing that angle was within 10 
        /// degrees) than to lurch at some randome angle.
        /// </summary>
        float targetAngle = 0;

        /// <summary>
        /// Steers the boat using AI.
        /// </summary>
        internal override void Move()
        {
            droneBrainIsAttachedTo.CameraAtFrontOfDrone.TakeSnapShot();
            Bitmap? cameraOutput = droneBrainIsAttachedTo.CameraAtFrontOfDrone.ImageFromCamera;

            if (cameraOutput == null) return; // no camera, no navigation by AI

            if (droneBrainIsAttachedTo.worldController.IsUnderManualControl) return;

            locationCentreOfTargetWithRespectToCamera = (int)Math.Round(brain.AIMapCameraImageIntoTargetX(CameraToTargetBrain.CameraPixelsToDoubleArray(ReturnBitmapFilteredToEdgesOnly(cameraOutput), null)));
            
            cameraOutput.Save(@"c:\temp\camera-output.png");

            // left of centre requires us to INCREASE the angle, right requires us to DECREASE
            float angleToTurn = -(locationCentreOfTargetWithRespectToCamera - 100f) / 100f * (float)Camera.c_HalfFOVAngleDegrees;

            // if we haven't locked on, and we are within 10 degrees of target centre, let's apply a lock-on
            if (!hasLocked && Math.Abs(angleToTurn) < 10)
            {
                hasLocked = true;
                droneBrainIsAttachedTo.worldController.Telemetry($">> MODE: LOCK ON @ {Math.Round(angleToTurn,2)} degrees");
                targetAngle = angleToTurn; // store the target angle
            }

            // avoid issues as we get very close, just keep on course.
            if (hasLocked && Math.Abs(angleToTurn - lastAngleToTurn) > 15f)
            {
                angleToTurn = targetAngle;
            }

            lastAngleToTurn = angleToTurn;

            // rotate the drone boat +/-10 degrees. Why? Because boats cannot spin on the spot due to water viscosity.
            
            float desiredAngle = droneBrainIsAttachedTo.AngleInDegrees + angleToTurn;
            float deltaAngle = Math.Abs(desiredAngle - droneBrainIsAttachedTo.AngleInDegrees).Clamp(0, ExplosiveDroneBoat.c_maxAngleOfDeflectionInDegreesPerMove);
            float angleInOptimalDirection = ((desiredAngle - droneBrainIsAttachedTo.AngleInDegrees + 540f) % 360) - 180f;
            
            droneBrainIsAttachedTo.AngleInDegrees = MathUtils.Clamp360(droneBrainIsAttachedTo.AngleInDegrees + deltaAngle * Math.Sign(angleInOptimalDirection));
        }

        /// <summary>
        /// Returns the camera output with a targe designator (upside down v).
        /// </summary>
        internal override Bitmap? Draw()
        {
            Bitmap? bitmap = droneBrainIsAttachedTo.CameraAtFrontOfDrone.CameraImage;

            if (bitmap is null) return null;

            using Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // shows where the AI thinks it should head to.
            OverlayAITargetDesignator(g);

            return bitmap;
        }

        /// <summary>
        /// Adds an upside down "v" to the graphics indicating the AI chosen value.
        /// </summary>
        /// <param name="g"></param>
        private void OverlayAITargetDesignator(Graphics g)
        {
            if (locationCentreOfTargetWithRespectToCamera == 0) return; // nothing to overlay

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            // "v" upside down
            g.DrawLines(Pens.White, new PointF[] {
                new PointF(2*locationCentreOfTargetWithRespectToCamera - 5, 91),
                new PointF(2*locationCentreOfTargetWithRespectToCamera, 86),
                new PointF(2*locationCentreOfTargetWithRespectToCamera + 5, 91)
            });
        }

        /// <summary>
        /// Converts the image into one where "edges" only are present.
        /// This is what the AI was trained on. If you change it, beware!!
        /// </summary>
        /// <param name="ImageToReplaceWithEdges"></param>
        /// <returns>Image with edges only.</returns>
        private static Bitmap ReturnBitmapFilteredToEdgesOnly(Bitmap ImageToReplaceWithEdges)
        {
            ImageUtils.LoadCurrentBitmap(ImageToReplaceWithEdges);
            ImageUtils.ConvertBitmapToGreyScale();

            return ImageUtils.ApplyRobertsFilter();
        }
    }
}