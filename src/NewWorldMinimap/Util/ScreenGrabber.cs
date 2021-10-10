using System.Windows.Forms;

namespace NewWorldMinimap.Util
{
    /// <summary>
    /// Provides logic for getting a screenshot of an image.
    /// </summary>
    public static class ScreenGrabber
    {
        /// <summary>
        /// Gets the screen count.
        /// </summary>
        /// <returns>The number of screens.</returns>
        public static int ScreenCount => Screen.AllScreens.Length;

        /// <summary>
        /// Takes the screenshot.
        /// </summary>
        /// <param name="screenIndex">Index of the screen.</param>
        /// <returns>The taken screenshot.</returns>
        public static System.Drawing.Rectangle GetScreenRect(int screenIndex = 0)
        {
            Screen[] screens = Screen.AllScreens;
            Screen screen = screenIndex >= 0 && screenIndex < screens.Length ? screens[screenIndex] : Screen.PrimaryScreen;
            return screen.Bounds;
        }

        /// <summary>
        /// Gets the index of the primary screen.
        /// </summary>
        /// <returns>The index of the primary screen.</returns>
        [SuppressMessage("Design", "CA1024", Justification = "Performs a computation.")]
        public static int GetPrimaryScreenIndex()
        {
            Screen[] screens = Screen.AllScreens;

            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == Screen.PrimaryScreen)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
