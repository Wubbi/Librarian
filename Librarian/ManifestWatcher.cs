using System;
using System.Threading;

namespace com.github.Wubbi.Librarian
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
        /// The current <see cref="LauncherInventory"/>
        /// </summary>
        public LauncherInventory CurrentInventory { get; private set; }

        /// <summary>
        /// Fires when the launchers version manifest received an update of some sort
        /// </summary>
        public event Action<LauncherInventory.Diff> ChangeInLauncherManifest;

        /// <summary>
        /// Creates a new <see cref="ManifestWatcher"/> that performs regular comparisons of the current manifest file with the known one
        /// </summary>
        /// <param name="initialComparison">The <see cref="LauncherInventory"/> comparisons are made with initially or null to download the live version</param>
        public ManifestWatcher(LauncherInventory initialComparison)
        {
            CurrentInventory = initialComparison ?? new LauncherInventory();

            _timer = new Timer(CheckLauncherManifest, null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Starts (or resets) the watcher with a specific time interval
        /// </summary>
        /// <param name="interval">The time in between update checks</param>
        public void Start(TimeSpan interval)
        {
            lock (_timer)
                _timer.Change(TimeSpan.Zero, interval);
        }

        /// <summary>
        /// Stops the watcher from performing any more checks
        /// </summary>
        public void Stop()
        {
            lock (_timer)
                _timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Called by timer to compare the manifest and trigger an event should a change be registered
        /// </summary>
        /// <param name="state"></param>
        private void CheckLauncherManifest(object state)
        {
            LauncherInventory liveInventory = new LauncherInventory();

            if (liveInventory.Equals(CurrentInventory))
                return;

            ChangeInLauncherManifest?.Invoke(new LauncherInventory.Diff(CurrentInventory, liveInventory));

            CurrentInventory = liveInventory;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
