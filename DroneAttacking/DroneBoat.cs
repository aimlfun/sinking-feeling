using SinkingFeelingPOC.DroneAttacking.CameraView;
using SinkingFeelingPOC.DroneAttacking.NavigationMethods;
using SinkingFeelingPOC.Map;
using SinkingFeelingPOC.Utils;
using SinkingFeelingPOC.World;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SinkingFeelingPOC.DroneAttacking
{
    /// <summary>
    /// Represents an explosive drone boat.
    /// </summary>
    internal class ExplosiveDroneBoat
    {
        /// <summary>
        /// Prevents drone boat going at an impossible speed.
        /// Speed taken from https://www.thesun.co.uk/wp-content/uploads/2022/09/TM-composite-Ukrainian-suicide-drone-boat-new-2.jpg
        /// </summary>
        internal const float c_droneCharacteristicsMaxPermissableSpeedinMS = 20.5f; // metres per second

        /// <summary>
        /// Rotate the drone boat max +/-1 degrees. Why? Because boats cannot spin on the spot due to water viscosity.
        /// </summary>
        internal const float c_maxAngleOfDeflectionInDegreesPerMove = 1; // 10 frames per second, that is 10 degrees per second

        /// <summary>
        /// The drone moves through "modes" in left to right order.
        /// </summary>
        internal enum DroneModes
        {
            awaitingOrders, // it does nothing. 
            followingGPS, // it moves from Odesa to Sevastopol using waypoints.
            searching, // searching it moves forward until target spotted, using AI to steer.
            targetLockon, // it has spotted the target and is within 10 degrees of centre
            exploded, // means the drone arrived at it's final destination.
            neutralised // the Corvette Admiral Makarov is available for dive tours post war end.
        };

        /// <summary>
        /// New drones always start off stationary waiting to be told where to go, and via.
        /// </summary>
        internal DroneModes Mode { get; set; } = DroneModes.awaitingOrders;

        /// <summary>
        /// These are the points the drone will take.
        /// </summary>
        PointF[] WayPoints = Array.Empty<PointF>();

        /// <summary>
        /// Which waypoint the drone is heading towards. (min of 1 waypoint - final destination).
        /// </summary>
        internal int NextWayPointIndicator = 0;

        /// <summary>
        /// Provides the way point to head towards. It includes detecting when to switch to the next waypoint.
        /// </summary>
        internal PointF GetWaypoint
        {
            get
            {
                MapDefinition mapdef = worldController.mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn];

                // are we near the way point? If so, switch "next" way point to the one that follows.
                if (MathUtils.DistanceBetweenTwoPoints(RealWorldLocation, WayPoints[NextWayPointIndicator]) < Speed * mapdef.TimeMultiplier * 100 + 1)
                {
                    ++NextWayPointIndicator;
                }

                if (HasReachedDestination) return WayPoints[NextWayPointIndicator - 1]; // avoid index error

                return WayPoints[NextWayPointIndicator];
            }
        }

        /// <summary>
        /// Returns true if the drone boat has reached the destination (last way point).
        /// </summary>
        internal bool HasReachedDestination
        {
            get
            {
                return NextWayPointIndicator >= WayPoints.Length;
            }
        }

        /// <summary>
        /// Where the drone is in "real world location".
        /// </summary>
        internal PointF RealWorldLocation = new();

        /// <summary>
        /// The world controller that owns and manages the drone.
        /// </summary>
        internal WorldController worldController;

        /// <summary>
        /// Used to view what is front of the drone.
        /// </summary>
        internal Camera CameraAtFrontOfDrone;

        /// <summary>
        /// The class that controls the drone (AI or Satellite).
        /// </summary>
        internal NavigationBase navigation;

        /// <summary>
        /// The speed the drone is travelling.
        /// </summary>
        private float speed = 0;

        /// <summary>
        /// Setter/Getter for drone speed, capped at something sensible.
        /// </summary>
        internal float Speed
        {
            get
            {
                return speed;
            }

            set
            {
                // limit speed
                if (value > c_droneCharacteristicsMaxPermissableSpeedinMS) value = c_droneCharacteristicsMaxPermissableSpeedinMS;

                speed = value;
            }
        }

        /// <summary>
        /// Angle the boat is pointing.
        /// </summary>
        internal float AngleInDegrees = 330;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controller"></param>
        internal ExplosiveDroneBoat(WorldController controller)
        {
            if (controller is null) throw new ArgumentNullException(nameof(worldController), "Controller is required");

            worldController = controller;

            CameraAtFrontOfDrone = new(this, controller);

            Mode = DroneModes.awaitingOrders;
            navigation = new SatelliteNavigation(this);

            NextWayPointIndicator = 1;
        }

        /// <summary>
        /// Start the search.
        /// </summary>
        internal void StartSearchForTarget()
        {
            Mode = DroneModes.searching;

            worldController.Telemetry(">> SWITCHING TO AI");
            navigation = worldController.NavigationBrain; // add brain.

            Speed = c_droneCharacteristicsMaxPermissableSpeedinMS; // m/s

            // we need no way points
            WayPoints = Array.Empty<PointF>();
        }

        /// <summary>
        /// Draw the drone to the map.
        /// </summary>
        /// <param name="graphics"></param>
        internal void Draw(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            if (worldController.searchTriangle.Length > 0)
            {
                // search triangle is hatched
                HatchBrush hBrush = new(
                   HatchStyle.Percent05,
                   Color.FromArgb(20, 255, 255, 255),
                   Color.FromArgb(3, 255, 255, 255));

                graphics.FillPolygon(hBrush, ArrayOfRealWorldCoordinatesToPX(worldController.searchTriangle));

                Pen outline = new(Color.FromArgb(20, 255, 255, 255))
                {
                    DashStyle = DashStyle.Dash
                };

                graphics.DrawPolygon(outline, ArrayOfRealWorldCoordinatesToPX(worldController.searchTriangle));

                // we draw left and right in opposing colours, to enable us to confirm the code was correct.
                graphics.DrawLine(Pens.DarkRed,
                    worldController.mapManager.DictionaryOfMapsIndexedByMode[Map.MapManager.MapModes.zoomedIn].ConvertRealworldCoordinatesToPX(worldController.searchTriangle[0]), 
                    worldController.mapManager.DictionaryOfMapsIndexedByMode[Map.MapManager.MapModes.zoomedIn].ConvertRealworldCoordinatesToPX(worldController.searchTriangle[1]));

                graphics.DrawLine(Pens.DarkGreen,
                    worldController.mapManager.DictionaryOfMapsIndexedByMode[Map.MapManager.MapModes.zoomedIn].ConvertRealworldCoordinatesToPX(worldController.searchTriangle[0]),
                    worldController.mapManager.DictionaryOfMapsIndexedByMode[Map.MapManager.MapModes.zoomedIn].ConvertRealworldCoordinatesToPX(worldController.searchTriangle[2]));
            }

            if (worldController.shipPoints.Length > 0) graphics.DrawLines(Pens.Pink, ArrayOfRealWorldCoordinatesToPX(worldController.shipPoints));
        }

        /// <summary>
        /// Returns the realworld points into pixels.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private PointF[] ArrayOfRealWorldCoordinatesToPX(PointF[] points)
        {
            List<PointF> pointsInPX = new();

            foreach (PointF point in points)
            {
                pointsInPX.Add(worldController.mapManager.DictionaryOfMapsIndexedByMode[Map.MapManager.MapModes.zoomedIn].ConvertRealworldCoordinatesToPX(point));
            }

            return pointsInPX.ToArray();
        }

        /// <summary>
        /// Moves the drone using waypoints or AI.
        /// </summary>
        internal void Move()
        {
            if (Mode == DroneModes.exploded || Mode == DroneModes.neutralised) return; // cannot move

            navigation.Move();

            double angleInRadians = MathUtils.DegreesInRadians(AngleInDegrees);

            float mult = worldController.mapManager.DictionaryOfMapsIndexedByMode[worldController.mapManager.MapMode].TimeMultiplier;

            RealWorldLocation.X += (float)(Math.Cos(angleInRadians) * Speed * mult / 10f); // m/s, 10 = frame rate (100ms interval)
            RealWorldLocation.Y -= (float)(Math.Sin(angleInRadians) * Speed * mult / 10f);
        }

        /// <summary>
        /// Sets the way points and initialises the drone to go to the 2nd way point (#1).
        /// </summary>
        /// <param name="wayPoints"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal void SetWayPoints(List<PointF> wayPoints)
        {
            if (wayPoints.Count < 1) throw new ArgumentOutOfRangeException(nameof(wayPoints), "Drone requires a starting waypoint");

            WayPoints = wayPoints.ToArray();

            Mode = DroneModes.followingGPS;

            NextWayPointIndicator = 1;
        }
    }
}