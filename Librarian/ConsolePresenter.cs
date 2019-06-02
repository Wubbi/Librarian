using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace com.github.Wubbi.Librarian
{
    public class ConsolePresenter : IDisposable
    {
        private readonly ConsoleCanvas _canvas;

        private readonly ConsoleCanvas.Text _latestRelease;
        private readonly ConsoleCanvas.Text _latestSnapshot;
        private readonly ConsoleCanvas.Text _lastLibraryUpdate;

        private readonly List<ConsoleCanvas.Text> _logEntries;
        private int _logCounter;

        private readonly List<ConsoleCanvas.Text> _checkTextElements;
        private readonly ConsoleCanvas.Text _nextCheck;

        private readonly List<ConsoleCanvas.Text> _downloadTextElements;
        private readonly ConsoleCanvas.Text _downloadPercentage;
        private readonly ConsoleCanvas.Text _downloadReceivedVsTotal;
        private readonly ConsoleCanvas.Text _downloadTop;
        private readonly ConsoleCanvas.Text _downloadMain;


        public string LatestRelease
        {
            get => _latestRelease.Value;
            set
            {
                lock (_canvas)
                {
                    _latestRelease.Value = value;
                    _canvas.Redraw();
                }
            }
        }

        public string LatestSnapshot
        {
            get => _latestSnapshot.Value;
            set
            {
                lock (_canvas)
                {
                    _latestSnapshot.Value = value;
                    _canvas.Redraw();
                }
            }
        }

        public string LastLibraryUpdate
        {
            get => _lastLibraryUpdate.Value;
            set
            {
                lock (_canvas)
                {
                    _lastLibraryUpdate.Value = value;
                    _canvas.Redraw();
                }
            }
        }

        public string NextCheck
        {
            set
            {
                lock (_canvas)
                {
                    foreach (ConsoleCanvas.Text downloadTextElement in _downloadTextElements)
                        downloadTextElement.Visible = false;

                    foreach (ConsoleCanvas.Text checkTextElement in _checkTextElements)
                        checkTextElement.Visible = true;

                    _nextCheck.Value = value;

                    _canvas.Redraw();
                }
            }
        }

        public DownloadProgressEventArgs DownloadUpdate
        {
            set
            {
                lock (_canvas)
                {
                    foreach (ConsoleCanvas.Text downloadTextElement in _downloadTextElements)
                        downloadTextElement.Visible = true;

                    foreach (ConsoleCanvas.Text checkTextElement in _checkTextElements)
                        checkTextElement.Visible = false;

                    _downloadPercentage.Value = value.ProgressPercentage.ToString();
                    _downloadReceivedVsTotal.Value = $"{value.Received / 1_048_576D:F2}/{value.Total / 1_048_576D:F2}";

                    int steps = value.ProgressPercentage / 4;
                    string border = steps > 1 ? new string('_', steps - 1) : "";
                    string tip = steps > 0 ? "|" : "";

                    _downloadTop.Value = border;
                    _downloadMain.Value = border + tip;

                    _canvas.Redraw();
                }
            }
        }

        public ConsolePresenter()
        {
            _canvas = new ConsoleCanvas(60, 19)
            {
                Title = $"Librarian v{Assembly.GetExecutingAssembly().GetName().Version}"
            };

            _canvas.RegisterElement(new ConsoleCanvas.Rect("Separator", 8, 0, 7, _canvas.Width, '-', 1));
            _canvas.RegisterElement(new ConsoleCanvas.Rect("Border", 0, 0, _canvas.Height, _canvas.Width, '#', 1));

            _canvas.RegisterElement(new ConsoleCanvas.Text("Headline", 1, 1, 58, ConsoleCanvas.Alignment.Center))
                .Value = _canvas.Title;
            _canvas.RegisterElement(new ConsoleCanvas.Text("LastRelease", 3, 6, 29, ConsoleCanvas.Alignment.Left, '.'))
                .Value = "Latest Release";
            _canvas.RegisterElement(new ConsoleCanvas.Text("LastSnapshot", 4, 6, 29, ConsoleCanvas.Alignment.Left, '.'))
                .Value = "Latest Snapshot";
            _canvas.RegisterElement(new ConsoleCanvas.Text("LastUpdate", 6, 6, 29, ConsoleCanvas.Alignment.Left, '.'))
                .Value = "Last library update";

            _canvas.RegisterElement(new ConsoleCanvas.Text("Log", 8, 4, " Log "));
            _canvas.RegisterElement(new ConsoleCanvas.Text("Status", 14, 4, " Status "));

            _latestRelease = _canvas.RegisterElement(new ConsoleCanvas.Text("LastReleaseValue", 3, 35, 20));
            _latestSnapshot = _canvas.RegisterElement(new ConsoleCanvas.Text("LastSnapshotValue", 4, 35, 20));
            _lastLibraryUpdate = _canvas.RegisterElement(new ConsoleCanvas.Text("LastUpdateValue", 6, 35, 20));

            LatestRelease = "unknown";
            LatestSnapshot = "unknown";
            LastLibraryUpdate = "unknown";

            _logEntries = new List<ConsoleCanvas.Text>();
            for (int i = 0; i < 5; ++i)
                _logEntries.Add(_canvas.RegisterElement(new ConsoleCanvas.Text("LogEntry#" + i, 9 + i, 2, 57)));

            _logCounter = 0;

            _nextCheck = _canvas.RegisterElement(new ConsoleCanvas.Text("LibraryCheckTime", 16, 18, 23));
            _checkTextElements = new List<ConsoleCanvas.Text>
            {
                _canvas.RegisterElement(new ConsoleCanvas.Text("LibraryCheck", 16, 6, "Next check:")),
                _nextCheck
            };

            foreach (ConsoleCanvas.Text checkTextElement in _checkTextElements)
                checkTextElement.Visible = true;

            _downloadPercentage = _canvas.RegisterElement(new ConsoleCanvas.Text("DownloadPercentage", 16, 6, 3, ConsoleCanvas.Alignment.Right));
            _downloadReceivedVsTotal = _canvas.RegisterElement(new ConsoleCanvas.Text("DownloadReceivedVsTotal", 16, 38, 14, ConsoleCanvas.Alignment.Right));
            _downloadTop = _canvas.RegisterElement(new ConsoleCanvas.Text("DownloadTop", 15, 12, 24));
            _downloadMain = _canvas.RegisterElement(new ConsoleCanvas.Text("DownloadMain", 16, 12, 24, ConsoleCanvas.Alignment.Left, '-'));
            _downloadTextElements = new List<ConsoleCanvas.Text>()
            {
                _canvas.RegisterElement(new ConsoleCanvas.Text("PercentageStart", 16, 9, "% |")),
                _canvas.RegisterElement(new ConsoleCanvas.Text("PercentageEnd", 16, 36, "|")),
                _canvas.RegisterElement(new ConsoleCanvas.Text("ProgressUnit", 16, 53, "MB")),
                _downloadPercentage,
                _downloadReceivedVsTotal,
                _downloadTop,
                _downloadMain
            };

            foreach (ConsoleCanvas.Text downloadTextElement in _downloadTextElements)
                downloadTextElement.Visible = false;

            _canvas.Redraw();
        }

        public void AddLogEntry(DateTime timestamp, string message)
        {
            lock (_canvas)
            {
                ++_logCounter;

                if (_logCounter <= _logEntries.Count)
                {
                    _logEntries[_logCounter - 1].Value = $"{_logCounter,4} {message.Replace(Environment.NewLine, " / ")}";
                    return;
                }

                int i;
                for (i = 0; i < _logEntries.Count - 1; ++i)
                {
                    _logEntries[i].Value = _logEntries[i + 1].Value;
                }

                _logEntries[i].Value = $"{_logCounter,4} {message.Replace(Environment.NewLine, " / ")}";

                _canvas.Redraw();
            }
        }

        public void Dispose()
        {
            _canvas?.Dispose();
        }
    }
}
