using SinkingFeeling.ShipTarget;
using SinkingFeelingPOC.DroneAttacking;
using SinkingFeelingPOC.DroneAttacking.CameraView;
using SinkingFeelingPOC.DroneAttacking.NavigationMethods;
using SinkingFeelingPOC.Map;
using SinkingFeelingPOC.ShipTarget;
using SinkingFeelingPOC.Utils;
using System.Diagnostics;
using System.Security.Cryptography;
using static SinkingFeelingPOC.DroneAttacking.ExplosiveDroneBoat;
using Timer = System.Windows.Forms.Timer;

namespace SinkingFeelingPOC.World
{
    /// <summary>
    /// Controller of the simulation. Moves and draws the boat. Manages interaction with the map.
    /// </summary>
    internal class WorldController
    {
        /// <summary>
        /// A large font for the drone cam.
        /// </summary>
        private static Font s_cameMessagefont = new ("Segoe", 18);

        #region PRIVATE ATTRIBUTES
        /// <summary>
        /// true - it skips the waypoints, and starts at Sevastopol.
        /// false - it navigates via the way-points. (default)
        /// </summary>
        private const bool c_testingAI = false; // default: (false)

        /// <summary>
        /// true - the ship is at a specific location, enabling easier debugging.
        /// false - the ship is randomly positioned in the bay. 
        /// </summary>
        private const bool c_testingFixedShipPosition = false; // default: (false)

        /// <summary>
        /// true - Draws a a line pointing to target, and a perpendicular.
        /// false - no annotation drawn.
        /// </summary>
        private const bool c_showAdditionalDebugAnnotations = false; // default: (false)

        /// <summary>
        /// true - it draws the search triangle and target ship (Makarov).
        /// false - it draws neither. (default) 
        /// </summary>
        private bool showDroneBoatSearchPath = false; // default: (false)

        /// <summary>
        /// A windows timer firsts, to provide animation frames.
        /// </summary>
        private readonly Timer animationTimer = new();

        /// <summary>
        /// Where we paint the map.
        /// </summary>
        private readonly PictureBox pictureBoxToShowMap;

        /// <summary>
        /// Where we paint the drone cam.
        /// </summary>
        private readonly PictureBox pictureBoxToShowDroneCam;

        /// <summary>
        /// TBD, could output events.
        /// </summary>
        private readonly ListBox listBoxDroneCamData;

        /// <summary>
        /// Semaphore, to stop timer-ticks clashing. When true, a timer tick is ignored.
        /// </summary>
        private bool inAnimation = false;

        /// <summary>
        /// Contains the last location, so we can detect if the drone goes thru the target. 
        /// (Because the multiplier is too large).
        /// </summary>
        private PointF LastLocationOfDroneBoat = new();
        #endregion

        #region INTERNAL ATTRIBUTES
        /// <summary>
        /// This is the drone boat we animate.
        /// </summary>
        internal readonly ExplosiveDroneBoat droneBoat;
        
        /// <summary>
        /// The AI for controlling the drone boat to target.
        /// </summary>
        internal readonly AINavigation NavigationBrain;

        /// <summary>
        /// This manages dual map (plotting).
        /// </summary>
        internal readonly MapManager mapManager;

        /// <summary>
        /// "M" key switches between manual and AI.
        /// true - human is controlling drone-boat
        /// false - AI is controlling drone-boat.
        /// </summary>
        internal bool IsUnderManualControl = false;

        /// <summary>
        /// This is the Admiral Makarov target.
        /// </summary>
        internal readonly Ship targetShip;

        /// <summary>
        /// The triangle indicate the area the camera sees.
        /// </summary>
        internal PointF[] searchTriangle = Array.Empty<PointF>();

        /// <summary>
        /// The 3 points making up the ship (2 ends + middle).
        /// </summary>
        internal PointF[] shipPoints = Array.Empty<PointF>();
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        internal WorldController(PictureBox pictureBoxMap, PictureBox pictureBoxDroneCam, ListBox listBoxDroneCam)
        {
            // items the controller updates
            pictureBoxToShowMap = pictureBoxMap;
            pictureBoxToShowDroneCam = pictureBoxDroneCam;
            listBoxDroneCamData = listBoxDroneCam;

            // this manages maps, and associated scaling
            mapManager = new MapManager(this);

            // we need a "drone" boat
            droneBoat = new ExplosiveDroneBoat(this);

            // we need a target ship
            targetShip = new Ship(this);

            // it starts with 2 waypoints
            mapManager.AddWayPointInPixelCoordinates(MapManager.s_startPointAtOdesaInPXcoords);
            mapManager.AddWayPointInRealCoordinates(new PointF(148313.547f, 148153.359f)); // to avoid crashing into the coast

            // position the boat on the map at Odesa.
            droneBoat.RealWorldLocation = mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedOut].ConvertPXToRealWorldCoordinates(MapManager.s_startPointAtOdesaInPXcoords);

            pictureBoxMap.Image?.Dispose();
            pictureBoxMap.Image = mapManager.Plot(droneBoat, null); // ship is not visible at start

            NavigationBrain = new AINavigation(droneBoat);
        }

        /// <summary>
        /// Launches the drone.
        /// </summary>
        internal void LaunchDrone()
        {
            if (c_testingAI) // true=> jumps straight into the target search, skipping waypoints
            {
                Telemetry($">> --AI-TEST ONLY--.");
                SetDroneModeIntoSearchForTarget();
                Draw();
            }
            else
            {
                Telemetry($">> NAVIGATING VIA GPS.");
                // add final destination
                mapManager.AddWayPointInPixelCoordinates(MapManager.s_endPointAtSevastopolInPXcoords);

                droneBoat.SetWayPoints(mapManager.Waypoints);

                droneBoat.Mode = DroneModes.followingGPS;
            }

            animationTimer.Tick += Animate; // moves the drone.
            animationTimer.Interval = 100; // every 100ms

            ShowMessageOnDroneCam("NAVIGATING\nVIA\nWAYPOINTS");

            animationTimer.Start(); // moves the drone, animates things
        }

        /// <summary>
        /// Animate the simulation (when the timer tick occurs).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Animate(object? sender, EventArgs e)
        {
            if (inAnimation) return; // prevent re-entrancy when drawing a frame is too slow for frame rate

            inAnimation = true; // set lock

            Move();
            Draw();

            inAnimation = false; // release lock
        }

        /// <summary>
        /// Draw the boat on the map, and the camera view.
        /// </summary>
        internal void Draw()
        {
            if (droneBoat.Mode == DroneModes.neutralised) return;

            if (droneBoat.Mode == DroneModes.exploded)
            {
                showDroneBoatSearchPath = true;
                Exploded();
            }
            else
            {
                Bitmap? droneCamImage = droneBoat.navigation.Draw();

                if (droneCamImage is not null)
                {
                    pictureBoxToShowDroneCam.Image?.Dispose();
                    pictureBoxToShowDroneCam.Image = droneCamImage;
                }
            }

            // re-draw the map
            Bitmap updatedMapImage = mapManager.Plot(droneBoat, droneBoat.Mode == DroneModes.followingGPS ? null : targetShip); // ship is not visible at start

            using Graphics graphics = Graphics.FromImage(updatedMapImage);

            // if search triangle enabled, draw it. It only shows after hitting target, or explicitly set in the class attributes above.
            if (showDroneBoatSearchPath) droneBoat.Draw(graphics);

            // if zoomed in, we are searching or have found and are under AI control to hit target
            if (mapManager.MapMode == MapManager.MapModes.zoomedIn)
            {
                CameraViewTargetData data = AcquireTargetData; // camera takes picture

                MapDefinition mapdef = mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn];

                // enable this to see additional debug
                if (c_showAdditionalDebugAnnotations)
                {
                    DrawLineToTargetAndPerpendicular(graphics, data, mapdef);
                    PointF droneAt = mapdef.ConvertRealworldCoordinatesToPX(droneBoat.RealWorldLocation);
                    graphics.DrawString(Math.Round(data.PositionRelativeToCamera*Camera.CameraWidthPX).ToString(), new Font("Segoe", 10), Brushes.White, droneAt.X, droneAt.Y - 20);
                    PointF targetAt = mapdef.ConvertRealworldCoordinatesToPX(targetShip.RealWorldLocation);
                    graphics.DrawString(NavigationBrain.locationCentreOfTargetWithRespectToCamera.ToString(), new Font("Segoe", 10), Brushes.White, targetAt.X, targetAt.Y - 20);
                }
            }

            graphics.Flush();

            pictureBoxToShowMap.Image?.Dispose();
            pictureBoxToShowMap.Image = updatedMapImage;
        }

        /// <summary>
        /// Target (Makarov) was hit by explosive drone boat.
        /// </summary>
        private void Exploded()
        {
            ShowMessageOnDroneCam("SIGNAL LOST");

            listBoxDroneCamData.Items.Add(">> TARGET NEUTRALISED.");

            droneBoat.Mode = DroneModes.neutralised;
        }

        /// <summary>
        /// Re-use the camera screen to show a message.
        /// </summary>
        /// <param name="msg"></param>
        internal void ShowMessageOnDroneCam(string msg)
        {
            Bitmap droneCameraImage = new(Camera.CameraWidthPX, Camera.CameraHeightPX);

            using Graphics g = Graphics.FromImage(droneCameraImage);
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

            SizeF size = g.MeasureString(msg, s_cameMessagefont, Camera.CameraWidthPX);
            StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(msg, s_cameMessagefont, Brushes.White, new RectangleF(0, 0, Camera.CameraWidthPX, Camera.CameraHeightPX), sf);

            // we want realism, so we make it pixellated.
            droneCameraImage = Camera.DoublePixels(droneCameraImage);

            pictureBoxToShowDroneCam.Image?.Dispose();
            pictureBoxToShowDroneCam.Image = droneCameraImage;
        }

        /// <summary>
        /// Debug that draws a green line pointing to ship, and cyan that is perpendicular to the direction of the drone (because
        /// we trained on images assuming side on).
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="data"></param>
        /// <param name="mapdef"></param>
        private void DrawLineToTargetAndPerpendicular(Graphics graphics, CameraViewTargetData data, MapDefinition mapdef)
        {
            if (!data.IsVisible) return;

            graphics.DrawLine(
                pen: Pens.Green,
                pt1: mapdef.ConvertRealworldCoordinatesToPX(droneBoat.RealWorldLocation),
                pt2: mapdef.ConvertRealworldCoordinatesToPX(data.ShipLocated));

            PointF[] p = targetShip.GetLineThruShip(droneBoat.AngleInDegrees);

            graphics.DrawLine(
                pen: Pens.Cyan,
                pt1: mapdef.ConvertRealworldCoordinatesToPX(p[0]),
                pt2: mapdef.ConvertRealworldCoordinatesToPX(p[1]));
        }
        
        /// <summary>
        /// Moves the drone.
        /// </summary>
        private void Move()
        {
            droneBoat.Move();

            // gone from Odesa to Sevastopol, now time to burn some invader?
            if (droneBoat.Mode == DroneModes.followingGPS && droneBoat.HasReachedDestination)
            {
                SetDroneModeIntoSearchForTarget();
            }

            // if lockon then at some point we'll crash into the Makarov. This is detected using the following. 
            if (droneBoat.Mode == DroneModes.targetLockon)
            {
                if (MathUtils.DistanceBetweenTwoPoints(droneBoat.RealWorldLocation, targetShip.RealWorldLocation) < 20f)
                {
                    bool online = MathUtils.GetLineIntersection(LastLocationOfDroneBoat, droneBoat.RealWorldLocation, shipPoints[0], shipPoints[2], out PointF impact);

                    float mult = mapManager.DictionaryOfMapsIndexedByMode[mapManager.MapMode].TimeMultiplier;

                    // draw a long line thru the ship in both directions. If it intersects with the view triangle and the distance is close, it's a hit
                    if (online ||
                        MathUtils.DistanceBetweenTwoPoints(impact, targetShip.RealWorldLocation) <= droneBoat.Speed * mult ||
                        MathUtils.DistanceBetweenTwoPoints(shipPoints[0], targetShip.RealWorldLocation) <= droneBoat.Speed * mult ||
                        MathUtils.DistanceBetweenTwoPoints(shipPoints[1], targetShip.RealWorldLocation) <= droneBoat.Speed * mult ||
                        MathUtils.DistanceBetweenTwoPoints(shipPoints[2], targetShip.RealWorldLocation) <= droneBoat.Speed * mult
                        )
                    {
                        // avoid driving thru the ship
                        droneBoat.RealWorldLocation = targetShip.RealWorldLocation;
                        droneBoat.Mode = DroneModes.exploded; // kaboom;
                    }
                }
            }

            LastLocationOfDroneBoat = droneBoat.RealWorldLocation;
        }

        /// <summary>
        /// Drone boat becomes hunter, and target becomes known.
        /// </summary>
        private void SetDroneModeIntoSearchForTarget()
        {
            Telemetry($">> MODE: SEARCHING");

            PlaceTargetShipInRandomLocation();

            droneBoat.StartSearchForTarget();
            droneBoat.AngleInDegrees = 330;

            mapManager.SetMapMode(MapManager.MapModes.zoomedIn);

            droneBoat.RealWorldLocation = mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn].ConvertPXToRealWorldCoordinates(MapManager.s_startPointAtSevastopolInPXcoords);
        }

        /// <summary>
        /// We need the ship to be somewhere in the water, but not the same place every time.
        /// </summary>
        private void PlaceTargetShipInRandomLocation()
        {
            // place within circle for ship: diameter 172px @ 260, 338

            PointF shipRandomLocationCentre = mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn].ConvertPXToRealWorldCoordinates(new PointF(260, 338));
            PointF diameterXY = mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedIn].ConvertPXToRealWorldCoordinates(new PointF(172, 172));
            float radius = diameterXY.X / 2.5f;

            bool createdLocation = false;

            // pick either a hardcoded location (debugging) or random location (constrained to somewhere sensible)
            do
            {
                float x, y;

                if (c_testingFixedShipPosition) // using a known position allows repeated runs
                {
                    // For testing use (or add others)
                    // (1100.82f,2424.8593f is in the land, but requires extreme right turn
                    // (1442.82f,1378.85925f) is up to the left of the drone boat.
                    // (1476.82f,1357.8593f) is also up left

                    x = 1476f;
                    y = 1357.8593f;

                    Debug.WriteLine($"Target @ ({x},{y})");
                    Debug.WriteLine($"Target using FIXED location for debugging.");

                    createdLocation = true;
                    targetShip.RealWorldLocation = new PointF(x, y);
                }
                else
                {
                    // pick a random location in the square
                    x = RandomNumberGenerator.GetInt32(-(int)radius, (int)radius) + shipRandomLocationCentre.X;
                    y = RandomNumberGenerator.GetInt32(-(int)radius, (int)radius) + shipRandomLocationCentre.Y;

                    Debug.WriteLine($"Target @ ({x},{y})");

                    // is point it within the circle? (to make it "findable" the location is confined to a fixed position + radius.
                    if (MathUtils.DistanceBetweenTwoPoints(new PointF(x, y), shipRandomLocationCentre) < radius)
                    {
                        createdLocation = true;
                        targetShip.RealWorldLocation = new PointF(x, y);
                    }
                }
            } while (!createdLocation);
        }

        /// <summary>
        /// Rotates the drone to the right.
        /// </summary>
        internal void RotateDroneRight()
        {
            droneBoat.AngleInDegrees -= 3;
            //Telemetry($"new angle: {droneBoat.AngleInDegrees}");
        }

        /// <summary>
        /// Rotates the drone to the left.
        /// </summary>
        internal void RotateDroneLeft()
        {
            droneBoat.AngleInDegrees += 3;
            //Telemetry($"new angle: {droneBoat.AngleInDegrees}");
        }

        /// <summary>
        /// Slows the drone.
        /// </summary>
        internal void DecreaseDroneSpeed()
        {
            droneBoat.Speed -= 1; // km/s

            if (droneBoat.Speed < 0) droneBoat.Speed = 0; // no reverse
        }

        /// <summary>
        /// Speeds the drone up.
        /// </summary>
        internal void IncreaseDroneSpeed()
        {
            droneBoat.Speed += 1; // km/s
        }

        /// <summary>
        /// Writes a message to the "log" listbox
        /// </summary>
        /// <param name="message"></param>
        internal void Telemetry(string message)
        {
            listBoxDroneCamData.Items.Add(message);
        }

        /// <summary>
        /// Pauses/unpauses the simulation.
        /// </summary>
        internal void PauseUnPauseAnimation()
        {
            animationTimer.Enabled = !animationTimer.Enabled;
        }

        /// <summary>
        /// Checks to see if the Makarov is visible to the drone.
        /// True - drone can see the target.
        /// False - drone cannot see the target.
        /// </summary>
        internal bool ShipIsVisible
        {
            get
            {

                /* a camera sees in a trapezium with width of CCD/sensor (space occupied by pixels horizontally) plus lense determing field of vision.
                 * A 180 degree lense would make it easier to spot a target, but would potentially see more boats in single vision plus other things.
                 * We'll simplify using a triangle.
                 * 
                 *                             . A
                 *                         .
                 *                     .           # 
                 *                 .               # <- Makarov
                 *             .                   #
                 *        o> ----------------------- <- distanceToObject
                 *             .        
                 *                 . 
                 *                      .
                 *                          .
                 *                              . B
                 *                              
                 * We'll use a triangle approximation.
                 * 
                 * Makarov is 124.8M long about 828px long, 177 tall => height 26.6M tall!
                 * 
                 */

                // check within triangle

                // we have three points: (1) drone boat (2) A (3) B
                // (1) is the drone location.
                // (2) & 3 are the corners 
                // The points A & B are computed, as the boat rotates.

                // left of ship search triangle
                float angleLeftRadians = (float)MathUtils.DegreesInRadians(droneBoat.AngleInDegrees + Camera.c_HalfFOVAngleDegrees + 90);
                
                // right of ship search triangle
                float angleRightRadians = (float)MathUtils.DegreesInRadians(droneBoat.AngleInDegrees - Camera.c_HalfFOVAngleDegrees + 90);

                PointF A = new(
                    x: droneBoat.RealWorldLocation.X + (float)(Camera.c_distanceToObject * Math.Sin(angleLeftRadians)),
                    y: droneBoat.RealWorldLocation.Y + (float)(Camera.c_distanceToObject * Math.Cos(angleLeftRadians)));

                PointF B = new(
                    x: droneBoat.RealWorldLocation.X + (float)(Camera.c_distanceToObject * Math.Sin(angleRightRadians)),
                    y: droneBoat.RealWorldLocation.Y + (float)(Camera.c_distanceToObject * Math.Cos(angleRightRadians)));

                searchTriangle = new PointF[] { droneBoat.RealWorldLocation, A, B };

                // check points that make up the ship
                shipPoints = targetShip.GetPoints(droneBoat.AngleInDegrees);

                PointF[] lineThruShip = targetShip.GetLineThruShip(droneBoat.AngleInDegrees);

                bool shipInRange = false;

                // does ship "line" intersect with left edge of camera
                if (MathUtils.GetLineIntersection(searchTriangle[0], searchTriangle[1], lineThruShip[0], lineThruShip[1], out PointF _))
                {
                    shipInRange = true;
                }

                // does ship "line" intersect with right edge of camera
                if (MathUtils.GetLineIntersection(searchTriangle[0], searchTriangle[2], lineThruShip[0], lineThruShip[1], out PointF _))
                {
                    shipInRange = true;
                }

                // if the ship isn't visible, then no further calculation required
                if (!shipInRange) return false;

                // check within distance
                return true;
            }
        }

        /// <summary>
        /// Returns the distance between drone and target ship.
        /// </summary>
        internal float DistanceOfDroneToShipInRealCoordinates
        {
            get
            {
                return MathUtils.DistanceBetweenTwoPoints(droneBoat.RealWorldLocation, targetShip.RealWorldLocation);
            }
        }

        /// <summary>
        /// Returns a "camera view" object to help the camera paint the world correclty.
        /// </summary>
        internal CameraViewTargetData AcquireTargetData
        {
            get
            {
                CameraViewTargetData targetData = new();

                if (!ShipIsVisible) return targetData;

                // assumption IsShipVisibleHasBeenCalled prior to this

                // find where the ship intersects (i.e. how much is visible.
                if (shipPoints.Length == 0) return targetData; // no ship seen

                PointF[] lineThruShip = targetShip.GetLineThruShip(droneBoat.AngleInDegrees);

                // does the ship "line" go thru the search triangle left edge
                if (MathUtils.GetLineIntersection(searchTriangle[0], searchTriangle[1], lineThruShip[0], lineThruShip[1], out PointF intersectionOfLeftSearchAndShipLine))
                {
                    targetData.IsVisible = true;
                }

                // does the ship "line" go thru the search triangle right edge
                if (MathUtils.GetLineIntersection(searchTriangle[0], searchTriangle[2], lineThruShip[0], lineThruShip[1], out PointF intersectionOfRightSearchAndShipLine))
                {
                    targetData.IsVisible = true;
                }

                // size of viewport at distance : max distance
                float viewportsize = MathUtils.DistanceBetweenTwoPoints(intersectionOfLeftSearchAndShipLine, intersectionOfRightSearchAndShipLine);
                float scale = viewportsize / Camera.c_distanceToObject;

                targetData.HeightInRealWorld = Ship.c_shipHeightInMetres / scale;
                targetData.WidthInRealWorld = Ship.c_lengthOfShipInMetres / scale;

                // compute angle of drone to target centre
                float angleInRadiansOfDroneToShip = (float)Math.Atan2(
                    y: droneBoat.RealWorldLocation.Y - targetShip.RealWorldLocation.Y,
                    x: droneBoat.RealWorldLocation.X - targetShip.RealWorldLocation.X);

                angleInRadiansOfDroneToShip -= (float)MathUtils.DegreesInRadians(90);

                // computed angle from drone to target
                targetData.AngleOfTargetInDegrees = (float)MathUtils.RadiansInDegrees(angleInRadiansOfDroneToShip);

                PointF positionOfShipAtComputedAngleAndDistance = new(
                   x: droneBoat.RealWorldLocation.X + (float)(DistanceOfDroneToShipInRealCoordinates * Math.Sin(angleInRadiansOfDroneToShip)),
                   y: droneBoat.RealWorldLocation.Y - (float)(DistanceOfDroneToShipInRealCoordinates * Math.Cos(angleInRadiansOfDroneToShip)));

                // this is where ship is located based on our angle calc.
                targetData.ShipLocated = positionOfShipAtComputedAngleAndDistance;

                // if target can be seen, provide a position relative to camera
                if (MathUtils.DistanceBetweenTwoPoints(droneBoat.RealWorldLocation, targetShip.RealWorldLocation) < Camera.c_distanceToObject - 100)
                {
                    targetData.PositionRelativeToCamera = MathUtils.DistanceBetweenTwoPoints(targetData.ShipLocated, intersectionOfLeftSearchAndShipLine) / viewportsize;
                }

                return targetData;
            }
        }
    }
}