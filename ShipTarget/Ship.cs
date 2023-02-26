using SinkingFeelingPOC.DroneAttacking.CameraView;
using SinkingFeelingPOC.Utils;
using SinkingFeelingPOC.World;

namespace SinkingFeelingPOC.ShipTarget
{
    /// <summary>
    /// Represents the "ship" to be destroyed. i.e. the Corvette Admiral Makarov.
    /// </summary>
    internal class Ship
    {
        /// <summary>
        /// The length of the Corvette Admiral Makarov.
        /// </summary>
        internal const float c_lengthOfShipInMetres = 124.8f;

        /// <summary>
        /// The height of the Corvette Admiral Makarov.
        /// </summary>
        internal const float c_shipHeightInMetres = 3.7f;

        /// <summary>
        /// Where the boat is in "real world location".
        /// </summary>
        internal PointF RealWorldLocation = new();

        /// <summary>
        /// The controller that owns this sip.
        /// </summary>
        private readonly WorldController worldController;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="controller"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal Ship(WorldController controller)
        {
            if (controller is null) throw new ArgumentNullException(nameof(worldController), "Controller is required");

            worldController = controller;

            controller.Telemetry(">> TARGET: Admiral Makarov");
            controller.Telemetry(">> LOCATION: Sevastopol, Ukraine");
        }

        /// <summary>
        /// Approximation: describes ship in 3 points (bow,centre,stern)
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        internal PointF[] GetPoints(float angleInDegrees)
        {
            List<PointF> points = new();

            float shipHalfLength = Ship.c_lengthOfShipInMetres / 2;
            /*
             *    
             *  ¦
             *  ¦
             *  *---> angleInDegrees  < RealWorldLocation.X,Y
             *  ¦
             *  ¦
             *  
             */

            float angleARadians = (float)MathUtils.DegreesInRadians(-angleInDegrees + 90 + 90);
            float angleBRadians = (float)MathUtils.DegreesInRadians(-angleInDegrees - 90 + 90);

            points.Add(new PointF(x: RealWorldLocation.X + (float)(shipHalfLength * Math.Sin(angleARadians)),
                                  y: RealWorldLocation.Y - (float)(shipHalfLength * Math.Cos(angleARadians))));

            points.Add(RealWorldLocation);

            points.Add(new PointF(x: RealWorldLocation.X + (float)(shipHalfLength * Math.Sin(angleBRadians)),
                                  y: RealWorldLocation.Y - (float)(shipHalfLength * Math.Cos(angleBRadians))));


            return points.ToArray();
        }


        /// <summary>
        /// Approximation: Line thru ship
        /// </summary>
        /// <param name="angleInDegrees"></param>
        /// <returns></returns>
        internal PointF[] GetLineThruShip(float angleInDegrees)
        {
            List<PointF> points = new();

            /*
             *    
             *  ¦
             *  ¦
             *  *---> angleInDegrees  < RealWorldLocation.X,Y
             *  ¦
             *  ¦
             *  
             */

            float angleARadians = (float)MathUtils.DegreesInRadians(-angleInDegrees + 90 + 90);
            float angleBRadians = (float)MathUtils.DegreesInRadians(-angleInDegrees - 90 + 90);

            points.Add(new PointF(x: RealWorldLocation.X + (float)(1.3f * Camera.c_distanceToObject * Math.Sin(angleARadians)),
                                  y: RealWorldLocation.Y - (float)(1.3f * Camera.c_distanceToObject * Math.Cos(angleARadians))));

            points.Add(new PointF(x: RealWorldLocation.X + (float)(1.3f * Camera.c_distanceToObject * Math.Sin(angleBRadians)),
                                  y: RealWorldLocation.Y - (float)(1.3f * Camera.c_distanceToObject * Math.Cos(angleBRadians))));


            return points.ToArray();
        }
    }
}