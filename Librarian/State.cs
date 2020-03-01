using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Librarian.Annotations;
using Librarian.Core;

namespace Librarian
{
    public class State : INotifyPropertyChanged
    {
        private View _current;
        private DateTime _nextOnlineCheck;
        private string _initCurrentPath;

        public enum View
        {
            Init,
            Main,
            Scan,
            Download
        }

        public View Current
        {
            get => _current;
            set
            {
                if (_current == value)
                    return;

                _current = value;
                OnPropertyChanged();
            }
        }

        public DateTime NextOnlineCheck
        {
            get => _nextOnlineCheck;
            set
            {
                if (value.Equals(_nextOnlineCheck)) return;
                _nextOnlineCheck = value;
                OnPropertyChanged();
            }
        }

        public LocalInventory Local { get; }

        public WebInventory Web { get; private set; }

        public DownloadState DownloadState { get; }

        public ScanState ScanState { get; }

        public string InitCurrentPath
        {
            get => _initCurrentPath;
            set
            {
                if (value == _initCurrentPath) return;
                _initCurrentPath = value;
                OnPropertyChanged();
            }
        }

        public State([NotNull] Settings settings)
        {
            Local = new LocalInventory(settings.LibraryRoot,settings.IgnoredAppTypes);

            DownloadState = new DownloadState();

            ScanState = new ScanState();

            Web = WebInventory.Empty;
        }

        public void UpdateWeb()
        {
            NextOnlineCheck = DateTime.MinValue;
            string manifest = DownloadState.DownloadStringAsync(WebInventory.LauncherManifest).ConfigureAwait(false).GetAwaiter().GetResult();
            Web = new WebInventory(manifest);
        }

        public void ValidateLibrary()
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
