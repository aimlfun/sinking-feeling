using SinkingFeelingPOC.Utils;

namespace SinkingFeelingPOC.DroneAttacking.NavigationMethods
{
    /// <summary>
    /// Navigates the drone by "satellite".
    /// </summary>
    internal class SatelliteNavigation : NavigationBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="whatCameraIsAttachedTo"></param>
        internal SatelliteNavigation(ExplosiveDroneBoat whatCameraIsAttachedTo) : base(whatCameraIsAttachedTo)
        {
        }

        /// <summary>
        /// Steers the boat towards the current waypoint. Upon reaching it sets the target to be the next way point.
        /// </summary>
        internal override void Move()
        {
            // note: the complication with images being upside down cartesian, messes with our angles.

            // math's to decide angle and move
            PointF waypoint = droneBrainIsAttachedTo.GetWaypoint;

            // in the real world, the drone coordinates would come from a satellite.
            float distX = waypoint.X - droneBrainIsAttachedTo.RealWorldLocation.X;
            float distY = waypoint.Y - droneBrainIsAttachedTo.RealWorldLocation.Y;

            // use ATAN2 to compute the angle.
            float desiredAngle = (float)MathUtils.RadiansInDegrees(Math.Atan2(-distY, distX));

            if (desiredAngle < 0) desiredAngle += 360;

            // limit the angle to +/-10 degrees
            float deltaAngle = Math.Abs(desiredAngle - droneBrainIsAttachedTo.AngleInDegrees).Clamp(0, ExplosiveDroneBoat.c_maxAngleOfDeflectionInDegreesPerMove);

            // turn the optimal direction
            float angleInOptimalDirection = ((desiredAngle - droneBrainIsAttachedTo.AngleInDegrees + 540f) % 360) - 180f;

            droneBrainIsAttachedTo.AngleInDegrees = MathUtils.Clamp360(droneBrainIsAttachedTo.AngleInDegrees + deltaAngle * Math.Sign(angleInOptimalDirection));
            droneBrainIsAttachedTo.Speed = ExplosiveDroneBoat.c_droneCharacteristicsMaxPermissableSpeedinMS;
        }

        /// <summary>
        /// Writes navigating by GPS to the drone cam.
        /// </summary>
        internal override Bitmap? Draw()
        {
            Bitmap? bitmap = droneBrainIsAttachedTo.CameraAtFrontOfDrone.ImageFromCamera;

            if (bitmap is null) return null;

            using Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            g.DrawString("Navigating by GPS", new Font("Arial", 12), Brushes.White, 30, 60);
            g.Flush();

            return bitmap;
        }
    }
}