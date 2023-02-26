using SinkingFeelingPOC.Map;
using SinkingFeelingPOC.Utils;
using SinkingFeelingPOC.World;

namespace SinkingFeeling
{
    /// <summary>
    /// Form for simulating a drone boat that targets a ship.
    /// </summary>
    public partial class FormMain : Form
    {
        /// <summary>
        /// This as its name suggests controls the "virtual" world, moving and drawing everything.
        /// </summary>
        private WorldController worldController;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// User clicked [Launch] button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLaunchDroneClick(object sender, EventArgs e)
        {
            // hide buttons, as once drone is launched user cannot add/remove waypoints or re-launch
            buttonRemoveWaypoint.Visible = false;
            buttonAddWayPoint.Visible = false;
            buttonLaunchDrone.Visible = false;

            worldController.LaunchDrone();
        }

        /// <summary>
        /// User clicked [Add Waypoint] button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAddWayPoint_Click(object sender, EventArgs e)
        {
            // provide instructions
            worldController.ShowMessageOnDroneCam("POINT TO\nMAP");

            // change the cursor, so use knows where they are clicking
            pictureBoxMap.Cursor = Cursors.Cross;

            // respond to mouse clicks on the map, to add waypoint
            pictureBoxMap.MouseDown += PictureBoxMap_MouseDownAddWayPoint;
        }

        /// <summary>
        /// User clicked on map, where they want the waypoint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBoxMap_MouseDownAddWayPoint(object? sender, MouseEventArgs e)
        {
            // the mouse-event-args are in pixels relative to top left of picture box,
            // we need them to be in "real" units (metres)
            worldController.mapManager.AddWayPointInPixelCoordinates(e.Location);

            // now we're done with click, reset cursor and remove the handler
            pictureBoxMap.Cursor = Cursors.Default;
            pictureBoxMap.MouseDown -= PictureBoxMap_MouseDownAddWayPoint;

            // re-draw map so that the waypoint is visible.
            pictureBoxMap.Image = worldController.mapManager.Plot(worldController.droneBoat, null); // ship is not visible at start

            // confirm action
            worldController.ShowMessageOnDroneCam("WAYPOINT\nADDED");
        }

        /// <summary>
        /// User clicked [Remove Waypoint].
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRemoveWaypoint_Click(object sender, EventArgs e)
        {
            // instructions, user has to click on the way point.
            worldController.ShowMessageOnDroneCam("CLICK\n TO REMOVE");

            // we use "hand" pointer for them to designate where to remove the point
            pictureBoxMap.Cursor = Cursors.Hand;

            // add on click so user can point and click on existing way point.
            pictureBoxMap.MouseDown += PictureBoxMap_MouseDownRemoveWayPoint;
        }

        /// <summary>
        /// User is removing a way point, and has clicked on the map.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBoxMap_MouseDownRemoveWayPoint(object? sender, MouseEventArgs e)
        {
            MapDefinition mapdef = worldController.mapManager.DictionaryOfMapsIndexedByMode[MapManager.MapModes.zoomedOut];

            bool foundWayPoint = false;

            int index = 0;

            // we didn't do anything fancy, there are no clickable items so it requires us to
            // search for any near where the user clicked.

            foreach (PointF p in worldController.mapManager.Waypoints)
            {
                // way points are real world coordinates, mouse-event-args locations are pixels, so we need to convert
                PointF z = mapdef.ConvertRealworldCoordinatesToPX(p);

                // we look for any within a 20 px circle around the waypoint.
                if (MathUtils.DistanceBetweenTwoPoints(e.Location, z) < 20)
                {
                    // prevent user clicking on the start point (end point isn't in the list)
                    if (z.X != MapManager.s_startPointAtOdesaInPXcoords.X &&
                        z.Y != MapManager.s_startPointAtOdesaInPXcoords.Y)
                    {
                        foundWayPoint = true;
                        break;
                    }
                }

                ++index;
            }

            // inform the user of outcome. They may not have clicked on a waypoint
            if (foundWayPoint)
            {
                worldController.mapManager.RemoveWayPointByIndex(index);
                worldController.ShowMessageOnDroneCam("WAYPOINT\nREMOVED");

                // repaint map, as we've removed a waypoint
                pictureBoxMap.Image = worldController.mapManager.Plot(worldController.droneBoat, null); // ship is not visible at start
            }
            else
            {
                worldController.ShowMessageOnDroneCam("NOT FOUND");
            }

            // either way, we reset the cursor and unattach the event handler
            pictureBoxMap.Cursor = Cursors.Default;
            pictureBoxMap.MouseDown -= PictureBoxMap_MouseDownRemoveWayPoint;
        }

        /// <summary>
        /// Certain key-presses perform useful activities, like move drone or pause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                // keys to control the drone.
                case Keys.Left:
                    worldController.RotateDroneLeft();
                    break;

                case Keys.Right:
                    worldController.RotateDroneRight();
                    break;

                case Keys.Up:
                    worldController.IncreaseDroneSpeed();
                    break;

                case Keys.Down:
                    worldController.DecreaseDroneSpeed();
                    break;

                // pauses the simulation
                case Keys.P:
                    worldController.PauseUnPauseAnimation();
                    break;

                // manual mode, AI doesn't steer, user can steer.
                // use it to check the "camera" looks correct.
                case Keys.M:
                    worldController.IsUnderManualControl = !worldController.IsUnderManualControl;
                    break;
            }
        }

        /// <summary>
        /// On load, we can initialise our world controller.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            Show();

            // the controller updates these
            worldController = new(pictureBoxMap, pictureBoxDroneCam, listBoxDroneCam);

            // first step user adds waypoints
            worldController.ShowMessageOnDroneCam("DEFINE ROUTE");
        }
    }
}