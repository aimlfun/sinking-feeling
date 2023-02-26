namespace SinkingFeelingPOC.Utils
{
    /// <summary>
    /// Maths related utility functions.
    /// </summary>
    internal static class MathUtils
    {
        /// <summary>
        /// Determine a point rotated by an angle around an origin.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="origin"></param>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        internal static PointF RotatePointAboutOrigin(PointF point, PointF origin, double angleInDegrees)
        {
            return RotatePointAboutOriginInRadians(point, origin, DegreesInRadians(angleInDegrees));
        }

        /// <summary>
        /// Determine a point rotated by an angle around an origin.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="origin"></param>
        /// <param name="angleInRadians"></param>
        /// <returns></returns>
        internal static PointF RotatePointAboutOriginInRadians(PointF point, PointF origin, double angleInRadians)
        {
            double cos = Math.Cos(angleInRadians);
            double sin = Math.Sin(angleInRadians);

            float dx = point.X - origin.X;
            float dy = point.Y - origin.Y;

            // standard maths for rotation.
            return new PointF((float)(cos * dx - sin * dy + origin.X),
                              (float)(sin * dx + cos * dy + origin.Y)
            );
        }

        /// <summary>
        /// Logic requires radians but we track angles in degrees, this converts.
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        internal static double DegreesInRadians(double angle)
        {
            return Math.PI * angle / 180;
        }

        /// <summary>
        /// Converts radians into degrees. 
        /// One could argue, WHY not just use degrees? Preference. Degrees are more intuitive than 2*PI offset values.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        internal static double RadiansInDegrees(double radians)
        {
            // radians = PI * angle / 180
            // radians * 180 / PI = angle
            return radians * 180F / Math.PI;
        }

        /// <summary>
        /// Computes the distance between 2 points using Pythagoras's theorem a^2 = b^2 + c^2.
        /// </summary>
        /// <param name="pt1">First point.</param>
        /// <param name="pt2">Second point.</param>
        /// <returns></returns>
        internal static float DistanceBetweenTwoPoints(PointF pt1, PointF pt2)
        {
            float dx = pt2.X - pt1.X;
            float dy = pt2.Y - pt1.Y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Returns true if the lines intersect, otherwise false and intersectionPoint = PointF(-999,-999). 
        /// In addition, if the lines intersect the intersection point is stored in intersectionPoint.
        /// </summary>
        /// <param name="line1P1"></param>
        /// <param name="line1P2"></param>
        /// <param name="line2P1"></param>
        /// <param name="line2P2"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns></returns>
        public static bool GetLineIntersection(PointF line1P1,
                                               PointF line1P2,
                                               PointF line2P1,
                                               PointF line2P2,
                                               out PointF intersectionPoint)
        {
            float s1_x, s1_y, s2_x, s2_y;

            s1_x = line1P2.X - line1P1.X;
            s1_y = line1P2.Y - line1P1.Y;

            s2_x = line2P2.X - line2P1.X;
            s2_y = line2P2.Y - line2P1.Y;

            float s = (-s1_y * (line1P1.X - line2P1.X) + s1_x * (line1P1.Y - line2P1.Y)) / (-s2_x * s1_y + s1_x * s2_y);
            float t = (s2_x * (line1P1.Y - line2P1.Y) - s2_y * (line1P1.X - line2P1.X)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected
                intersectionPoint = new PointF(line1P1.X + t * s1_x, line1P1.Y + t * s1_y);

                return true;
            }

            intersectionPoint = new PointF(-999, -999);

            return false; // No collision
        }

        /// <summary>
        /// Ensures value is within a circle angle.
        /// </summary>
        /// <returns></returns>
        internal static float Clamp360(float val)
        {
            while (val < 0) val += 360;
            while (val > 360) val -= 360;

            return val;
        }

        /// <summary>
        /// Returns a value between min and max (never outside of).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
            {
                return min;
            }

            if (val.CompareTo(max) > 0)
            {
                return max;
            }

            return val;
        }
    }
}