using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librarian.Annotations;
using Librarian.Core;

namespace Librarian
{
    public class DownloadState : INotifyPropertyChanged
    {
        private string _url;
        private long _totalSize;
        private long _currentSize;
        private double _speed;
        private double _smoothSpeed;
        private double _completion;

        private long _queueTotalSize;

        public string Url
        {
            get => _url;
            private set
            {
                if (value.Equals(_url)) return;
                _url = value;
                OnPropertyChanged();
            }
        }

        public long TotalSize
        {
            get => _totalSize;
            private set
            {
                if (value == _totalSize) return;
                _totalSize = value;
                OnPropertyChanged();
            }
        }

        public long CurrentSize
        {
            get => _currentSize;
            private set
            {
                if (value == _currentSize) return;
                _currentSize = value;
                OnPropertyChanged();
            }
        }

        public double Speed
        {
            get => _speed;
            private set
            {
                if (value.Equals(_speed)) return;
                _speed = value;
                OnPropertyChanged();
            }
        }

        public double SmoothSpeed
        {
            get => _smoothSpeed;
            private set
            {
                if (value.Equals(_smoothSpeed)) return;
                _smoothSpeed = value;
                OnPropertyChanged();
            }
        }

        public double Completion
        {
            get => _completion;
            private set
            {
                if (value.Equals(_completion)) return;
                _completion = value;
                OnPropertyChanged();
            }
        }

        public long QueueTotalSize
        {
            get => _queueTotalSize;
            private set
            {
                if (value.Equals(_queueTotalSize)) return;
                _queueTotalSize = value;
                OnPropertyChanged();
            }
        }

        public int QueuedItems => _plannedDownloads.Count;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<string, long> _plannedDownloads;

        private readonly Average _average;

        private long _fullQueueTarget;
        private double _fullQueueQueueCompletion;
        private bool _fullQueuePercentageByItems;

        public double FullQueueCompletion
        {
            get => _fullQueueQueueCompletion;
            set
            {
                if (value.Equals(_fullQueueQueueCompletion)) return;
                _fullQueueQueueCompletion = value;
                OnPropertyChanged();
            }
        }

        public DownloadState()
        {
            Url = "";
            TotalSize = 0L;
            CurrentSize = 0L;
            Speed = 0D;
            Completion = 0D;
            _plannedDownloads = new Dictionary<string, long>();
            _average = new Average(32);
        }

        public async Task<WebResource> DownloadAsync(string url, CancellationToken token)
        {
            if (_plannedDownloads.Remove(url))
            {
                QueueTotalSize = _plannedDownloads.Values.Sum();
                OnPropertyChanged(nameof(QueuedItems));
            }

            _average.Reset();

            Url = url;
            WebResource webResource = await WebResource.LoadAsync(url, token, UpdateCallback).ConfigureAwait(false);

            Url = "";
            TotalSize = 0L;
            CurrentSize = 0L;
            Speed = 0D;
            Completion = 0D;

            return webResource;
        }

        public async Task<string> DownloadStringAsync(string url)
        {
            using WebResource resource = await DownloadAsync(url, CancellationToken.None);
            string downloadedString = resource.AsString(Encoding.UTF8);

            return downloadedString;
        }

        private void UpdateCallback(DownloadProgress progress)
        {
            TotalSize = progress.TotalSize;
            CurrentSize = progress.CurrentSize;
            Speed = progress.Speed;
            Completion = progress.Completion;
            SmoothSpeed = _average.Next(progress.Speed);

            if (_fullQueueTarget <= 0)
            {
                FullQueueCompletion = 0D;
                return;
            }

            if (_fullQueuePercentageByItems)
                FullQueueCompletion = (_fullQueueTarget - QueuedItems - 1) / (double)_fullQueueTarget;
            else
                FullQueueCompletion = (_fullQueueTarget - QueueTotalSize - (TotalSize - CurrentSize)) / (double)_fullQueueTarget;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PlanDownload(string url, long size)
        {
            _plannedDownloads[url] = size;
            QueueTotalSize = _plannedDownloads.Values.Sum();
            OnPropertyChanged(nameof(QueuedItems));

            _fullQueuePercentageByItems = QueuedItems > 0 && QueueTotalSize <= 0;

            if (_fullQueuePercentageByItems)
                _fullQueueTarget = QueuedItems;
            else
                _fullQueueTarget = QueueTotalSize;

            _fullQueueQueueCompletion = 0L;
        }
    }
}
