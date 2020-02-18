using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librarian.Annotations;
using Librarian.Core;

namespace Librarian
{
    public class ScanState : INotifyPropertyChanged
    {
        private int _totalVersions;
        private int _scannedVersions;
        private double _speed;
        private double _smoothSpeed;
        private double _completion;

        public int TotalVersions
        {
            get => _totalVersions;
            private set
            {
                if (value == _totalVersions) return;
                _totalVersions = value;
                OnPropertyChanged();
            }
        }

        public int ScannedVersions
        {
            get => _scannedVersions;
            private set
            {
                if (value == _scannedVersions) return;
                _scannedVersions = value;
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

        private readonly Average _average;

        public ScanState()
        {
            TotalVersions = 0;
            ScannedVersions = 0;
            Speed = 0D;
            Completion = 0D;
            _average = new Average(32);
        }

        public Task ValidateLibrary(LocalInventory localInventory, CancellationToken token)
            => localInventory.CheckIncompleteOrInvalidAsync(UpdateCallback, token);

        private void UpdateCallback(ScanProgress progress)
        {
            TotalVersions = progress.TotalVersions;
            ScannedVersions = progress.ScannedVersions;
            Speed = progress.Speed;
            Completion = progress.Completion;
            SmoothSpeed = _average.Next(progress.Speed);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
