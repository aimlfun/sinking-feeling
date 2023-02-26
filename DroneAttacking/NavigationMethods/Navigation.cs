namespace SinkingFeelingPOC.DroneAttacking.NavigationMethods
{
    /// <summary>
    /// Base class for navigation. Inherit from this to add navigation methods.
    /// </summary>
    internal class NavigationBase
    {
        /// <summary>
        /// What the camera sees depends on where it is.
        /// </summary>
        protected ExplosiveDroneBoat droneBrainIsAttachedTo;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="whatCameraIsAttachedTo"></param>
        internal NavigationBase(ExplosiveDroneBoat whatCameraIsAttachedTo)
        {
            droneBrainIsAttachedTo = whatCameraIsAttachedTo;
        }

        /// <summary>
        /// Responsible for adjusting angle / speed.
        /// </summary>
        /// <exception cref="Exception"></exception>
        internal virtual void Move()
        {
            throw new Exception("inherit this class and override");
        }

        /// <summary>
        /// Responsible for any drawing required.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal virtual Bitmap? Draw()
        {
            throw new Exception("inherit this class and override");
        }
    }
}