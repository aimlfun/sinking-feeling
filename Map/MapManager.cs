using SinkingFeelingPOC.DroneAttacking;
using SinkingFeelingPOC.ShipTarget;
using SinkingFeelingPOC.Utils;
using SinkingFeelingPOC.World;

namespace SinkingFeelingPOC.Map
{
    /// <summary>
    /// Manages the maps. We have two (zoomed-out, and zoomed-in).
    /// </summary>
    internal class MapManager
    {
        /// <summary>
        /// Contains waypoints in real coordinates (metres).
        /// </summary>
        private readonly List<PointF> wayPointsInRealCoordinates = new();

        /// <summary>
        /// The controller that owns the maps.
        /// </summary>
        private readonly WorldController worldController;

        /// <summary>
        /// true - draws the ship being targeted on the map.
        /// false - guess where the ship is
        /// </summary>
        private readonly bool showShipOnMap = false; // default: (false)

        /// <summary>
        /// Contains the list of maps available keyed on the map-mode.
        /// </summary>
        internal readonly Dictionary<MapModes, MapDefinition> DictionaryOfMapsIndexedByMode = new();

        /// <summary>
        /// Where the drone starts on the map. It was easier to work this position out using MSPaint. 
        /// </summary>
        internal static readonly PointF s_startPointAtOdesaInPXcoords = new(40, 46);

        /// <summary>
        /// Where the drone entes Sevastopol inlet.
        /// </summary>
        internal static readonly PointF s_endPointAtSevastopolInPXcoords = new(542, 534);

        /// <summary>
        /// Where drone starts on the search map. It was easier to work this position out using MSPaint. 
        /// </summary>
        internal static readonly PointF s_startPointAtSevastopolInPXcoords = new(110, 309);

        /// <summary>
        /// Used as keys to the maps.
        /// </summary>
        internal enum MapModes { zoomedOut, zoomedIn }

        /// <summary>
        /// Which map we're currently showing.
        /// </summary>
        internal MapModes MapMode = MapModes.zoomedOut;

        /// <summary>
        /// Returns a clone of the waypoints, to protect from the caller changing them.
        /// </summary>
        internal List<PointF> Waypoints
        {
            get
            {
                return new(wayPointsInRealCoordinates);
            }
        }

        /// <summary>
        /// Removes a waypoint using the index.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal void RemoveWayPointByIndex(int index)
        {
            if (index > wayPointsInRealCoordinates.Count || index == 0) throw new ArgumentOutOfRangeException(nameof(index));

            wayPointsInRealCoordinates.RemoveAt(index);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal MapManager(WorldController controller)
        {
            worldController = controller;


            // DIFF: PX [502,488] => Metres [217700, 207160f]
            DictionaryOfMapsIndexedByMode.Add(MapModes.zoomedOut, new MapDefinition(filePath: @"Map\Assets\Odessa-Sevastopol.png",
                                                                                   xscalePXdivM: 502f / 217700f,
                                                                                   yscalePXdivM: 488 / 207160f,
                                                                                   startPointInPixels: s_startPointAtOdesaInPXcoords,
                                                                                   500));

            // DIFF: PX [311,50] => Metres [1550, 239.92]
            DictionaryOfMapsIndexedByMode.Add(MapModes.zoomedIn, new MapDefinition(filePath: @"Map\Assets\Sevastopol-bay.png",
                                                                                    xscalePXdivM: 311f / 1550f,
                                                                                    yscalePXdivM: 50 / 239.92f,
                                                                                    startPointInPixels: s_startPointAtSevastopolInPXcoords,
                                                                                    2));
        }

        /// <summary>
        /// Adds a waypoint in "real coordinates".
        /// </summary>
        /// <param name="wayPointInRealCoordinates"></param>
        internal void AddWayPointInRealCoordinates(PointF wayPointInRealCoordinates)
        {
            wayPointsInRealCoordinates.Add(wayPointInRealCoordinates);
        }

        /// <summary>
        /// Adds a waypoint in "real coordinates" from a point in pixels.
        /// </summary>
        /// <param name="wayPointInPixelCoordinates"></param>
        internal void AddWayPointInPixelCoordinates(PointF wayPointInPixelCoordinates)
        {
            MapDefinition def = DictionaryOfMapsIndexedByMode[MapMode];

            wayPointsInRealCoordinates.Add(def.ConvertPXToRealWorldCoordinates(wayPointInPixelCoordinates));
        }

        /// <summary>
        /// Assigns a new map mode.
        /// </summary>
        /// <param name="mapMode"></param>
        internal void SetMapMode(MapModes mapMode)
        {
            MapMode = mapMode;
        }

        /// <summary>
        /// Draws the map annotated with drone and target positions.
        /// </summary>
        internal Bitmap Plot(ExplosiveDroneBoat? drone, Ship? target)
        {
            MapDefinition def = DictionaryOfMapsIndexedByMode[MapMode];

            Bitmap map = new(def.MapImage);

            float xscale = def.XScaleToConvertMetresIntoPixels;
            float yscale = def.YScaleToConvertMetresIntoPixels;

            using Graphics g = Graphics.FromImage(map);
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            List<PointF> waypoints = new(wayPointsInRealCoordinates);

            // waypoints don't include the destination, to make it easier to add way points
            // that means we need to inject here. so it shows the drone going to Sevastopol not
            // stopping at the last waypoint
            if (MapMode == MapModes.zoomedOut)
            {
                waypoints.Add(def.ConvertPXToRealWorldCoordinates(s_endPointAtSevastopolInPXcoords));
            }

            DrawWayPointsOnMap(xscale, yscale, g, waypoints);

            AddDroneToMap(drone, g, def);

            AddShipToMap(target, g, def, showShipOnMap);

            PointF location;

            // write speed multiplier at a sensible place for each map
            if (MapMode == MapModes.zoomedIn) location = new(10, 10); else location = new(10, 558);

            // show the speed, but also how much we accelerated "time" to avoid waiting"
            g.DrawString($"Drone speed: {worldController.droneBoat.Speed} km/s\nTime Multiplier: x{def.TimeMultiplier}", new Font("Sego", 16), Brushes.White, location);
            g.Flush();

            return map;
        }

        /// <summary>
        /// Draws the waypoints. They appear as blobs. The route in between is drawn as a dashed line.
        /// </summary>
        /// <param name="xscale"></param>
        /// <param name="yscale"></param>
        /// <param name="g"></param>
        /// <param name="waypoints"></param>
        private static void DrawWayPointsOnMap(float xscale, float yscale, Graphics g, List<PointF> waypoints)
        {
            // draw the lines between waypoint before drawing blobs for the waypoints.
            if (waypoints.Count > 1)
            {
                // convert points into map image coordinate
                List<PointF> pointsInMapImageCoords = new();

                foreach (PointF point in waypoints)
                {
                    pointsInMapImageCoords.Add(new PointF(point.X * xscale, point.Y * yscale));
                }

                Pen dottedPen = new(Color.FromArgb(100, 255, 255, 255))
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
                };

                g.DrawLines(dottedPen, pointsInMapImageCoords.ToArray());
            }

            // plot a blob for the way point
            foreach (PointF point in waypoints)
            {
                g.FillEllipse(Brushes.Yellow, new RectangleF(point.X * xscale - 3, point.Y * yscale - 3, 6, 6));
            }
        }

        /// <summary>
        /// Draws an indicator of where the "target" ship is to the map.
        /// If show ship visibly, it's RED, in your face. Otherwise a "subtle" indicator is added.
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="g"></param>
        /// <param name="mapDefinition"></param>
        private static void AddShipToMap(Ship? ship, Graphics g, MapDefinition mapDefinition, bool showShipVisibly)
        {
            if (ship is null) return; // nothing to draw

            PointF shipInPX = mapDefinition.ConvertRealworldCoordinatesToPX(ship.RealWorldLocation);

            Brush brush = showShipVisibly ? Brushes.Red : Brushes.Gray;

            // red box
            g.FillRectangle(brush, shipInPX.X - 1, shipInPX.Y - 1, 3, 3);
        }

        /// <summary>
        /// Draws a triangle indicator of where the drone is to the map.
        /// </summary>
        /// <param name="droneBoat"></param>
        /// <param name="g"></param>
        /// <param name="mapDefinition"></param>
        private static void AddDroneToMap(ExplosiveDroneBoat? droneBoat, Graphics g, MapDefinition mapDefinition)
        {
            if (droneBoat is null) return; // nothing to draw

            PointF droneBoatInPX = mapDefinition.ConvertRealworldCoordinatesToPX(droneBoat.RealWorldLocation);

            // a triangle
            float x1 = 5;
            float y1 = 0;

            float x2 = -5;
            float y2 = -5;

            float x3 = -5;
            float y3 = 5;

            // standard rotate triangle around 0,0 origin.

            PointF p1 = MathUtils.RotatePointAboutOrigin(new PointF(x1, y1), new PointF(0, 0), -droneBoat.AngleInDegrees);
            PointF p2 = MathUtils.RotatePointAboutOrigin(new PointF(x2, y2), new PointF(0, 0), -droneBoat.AngleInDegrees);
            PointF p3 = MathUtils.RotatePointAboutOrigin(new PointF(x3, y3), new PointF(0, 0), -droneBoat.AngleInDegrees);

            g.FillPolygon(Brushes.Red, new PointF[] {
                new(p1.X+ droneBoatInPX.X,p1.Y+ droneBoatInPX.Y),
                new(p2.X+ droneBoatInPX.X,p2.Y+ droneBoatInPX.Y),
                new(p3.X+ droneBoatInPX.X,p3.Y+ droneBoatInPX.Y)
            });
        }
    }
}