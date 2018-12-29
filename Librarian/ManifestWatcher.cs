using System;
using System.Threading;

namespace Librarian
{
    /// <summary>
    /// Continuously reads the launcher data and triggers events
    /// </summary>
    public class ManifestWatcher : IDisposable
    {
        /// <summary>
        /// The timer which triggers a comparison of the known with the current manifest
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The <see cref="LauncherInventory"/> which is compared with the current one
        /// </summary>
        private LauncherInventory _comparisonInventory;

        /// <summary>
        /// Fires when the launchers version manifest received an update of some sort
        /// </summary>
        public event Action<LauncherInventory> ChangeInLauncherManifest;

        /// <summary>
        /// Creates a new <see cref="ManifestWatcher"/> that performs regular comparisons of the current manifest file with the known one
        /// </summary>
        public ManifestWatcher()
        {
            _comparisonInventory = new LauncherInventory();

            _timer = new Timer(CheckLauncherManifest, null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Starts (or resets) the watcher with a specific time interval
        /// </summary>
        /// <param name="interval">The time in between update checks</param>
        public void Start(TimeSpan interval)
        {
            _timer.Change(TimeSpan.Zero, interval);
        }

        /// <summary>
        /// Stops the watcher from performing any more checks
        /// </summary>
        public void Stop()
        {
            _timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Called by timer to compare the manifest and trigger an event should a change be registered
        /// </summary>
        /// <param name="state"></param>
        private void CheckLauncherManifest(object state)
        {
            LauncherInventory currentInventory = new LauncherInventory();

            if (currentInventory.Equals(_comparisonInventory))
                return;

            _comparisonInventory = currentInventory;
            ChangeInLauncherManifest?.Invoke(currentInventory);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
