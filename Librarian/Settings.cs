namespace Librarian
{
    /// <summary>
    /// Stores all the settings made with the settings file
    /// </summary>
    ///TODO: Fill in properties as the project grows, handle loading afterwards
    public class Settings
    {
        /// <summary>
        /// The time interval in seconds at which the launcher manifest file is reread to see if any updates were made
        /// </summary>
        public int ManifestRefreshRate { get; private set; }

        /// <summary>
        /// The path to the directory in which the library is to be maintained
        /// </summary>
        public string LibraryPath { get; private set; }
    }
}
