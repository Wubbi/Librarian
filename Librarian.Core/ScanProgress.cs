using System;

namespace Librarian.Core
{
    public delegate void ScanProgressUpdate(ScanProgress progress);

    public class ScanProgress
    {
        public int TotalVersions { get; }

        public int ScannedVersions { get; }

        public double Speed { get; }

        public double Completion => TotalVersions == 0 ? 0D : (double)ScannedVersions / TotalVersions;

        private ScanProgress(int totalVersions, int scannedVersions, double speed)
        {
            TotalVersions = totalVersions;
            ScannedVersions = scannedVersions;
            Speed = speed;
        }

        public TimeSpan GetTimeLeft()
        {
            if (double.IsNormal(Speed) && Speed > 0)
                return TimeSpan.FromSeconds((TotalVersions - ScannedVersions) / Speed);

            return TimeSpan.MaxValue;
        }

        internal class Factory
        {
            private readonly int _totalVersions;

            private DateTime _lastUpdateTime;

            private int _lastCount;

            private readonly ValueTuple<long, double>[] _measurements;

            private int _measurementHead;

            internal Factory(int totalVersions)
            {
                _totalVersions = totalVersions;
                _lastUpdateTime = DateTime.Now;
                _lastCount = 0;
                _measurements = new ValueTuple<long, double>[16];
                _measurementHead = 0;
            }

            internal ScanProgress Update(int completed)
            {
                DateTime now = DateTime.Now;

                double timeDiff = (now - _lastUpdateTime).TotalSeconds;

                if (timeDiff > 0.5)
                {
                    _measurements[_measurementHead] = new ValueTuple<long, double>(completed - _lastCount, timeDiff);
                    _measurementHead = (_measurementHead + 1) % _measurements.Length;

                    _lastUpdateTime = now;
                    _lastCount = completed;
                }

                long totalSizeDiff = 0L;
                double totalTimeDiff = 0D;
                for (int i = 0; i < _measurements.Length; ++i)
                {
                    totalSizeDiff += _measurements[i].Item1;
                    totalTimeDiff += _measurements[i].Item2;
                }

                return new ScanProgress(_totalVersions, completed, totalTimeDiff <= 0 ? 0 : totalSizeDiff / totalTimeDiff);
            }
        }
    }
}
