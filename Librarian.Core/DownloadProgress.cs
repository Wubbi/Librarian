using System;

namespace Librarian.Core
{
    public delegate void DownloadProgressUpdate(DownloadProgress progress);

    /// <summary>
    /// The progress of an active download
    /// </summary>
    public class DownloadProgress
    {
        /// <summary>
        /// The total size of the data that is being loaded in Bytes
        /// </summary>
        public long TotalSize { get; }

        /// <summary>
        /// The size in Bytes of the data that has been loaded so far
        /// </summary>
        public long CurrentSize { get; }

        /// <summary>
        /// The average speed of the download in Bytes per second
        /// </summary>
        public double Speed { get; }

        /// <summary>
        /// The percentage of completion for the ongoing download (as a value between 0 and 1)
        /// </summary>
        public double Completion => TotalSize == 0 ? 0D : (double)CurrentSize / TotalSize;

        private DownloadProgress(long totalSize, long currentSize, double speed)
        {
            TotalSize = totalSize;
            CurrentSize = currentSize;
            Speed = speed;
        }

        /// <summary>
        /// Returns an approximation for the time left to finish the download based on the current speed
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTimeLeft()
        {
            if (double.IsNormal(Speed) && Speed > 0)
                return TimeSpan.FromSeconds((TotalSize - CurrentSize) / Speed);

            return TimeSpan.MaxValue;
        }

        internal class Factory
        {
            private readonly long _totalSize;

            private DateTime _lastUpdateTime;

            private long _lastSize;

            private readonly ValueTuple<long, double>[] _measurements;

            private int _measurementHead;

            internal Factory(long totalSize)
            {
                _totalSize = totalSize;
                _lastUpdateTime = DateTime.Now;
                _lastSize = 0L;
                _measurements = new ValueTuple<long, double>[32];
                _measurementHead = 0;
            }

            internal DownloadProgress Update(long completed)
            {
                DateTime now = DateTime.Now;

                double timeDiff = (now - _lastUpdateTime).TotalSeconds;

                if (timeDiff > 0.2)
                {
                    _measurements[_measurementHead] = new ValueTuple<long, double>(completed - _lastSize, timeDiff);
                    _measurementHead = (_measurementHead + 1) % _measurements.Length;

                    _lastUpdateTime = now;
                    _lastSize = completed;
                }

                long totalSizeDiff = 0L;
                double totalTimeDiff = 0D;
                for (int i = 0; i < _measurements.Length; ++i)
                {
                    totalSizeDiff += _measurements[i].Item1;
                    totalTimeDiff += _measurements[i].Item2;
                }

                return new DownloadProgress(_totalSize, completed, totalTimeDiff <= 0 ? 0 : totalSizeDiff / totalTimeDiff);
            }
        }
    }
}
