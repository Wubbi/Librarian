using System;
using System.Globalization;
using System.Text;
using queuepacked.ConsoleUI;
using queuepacked.ConsoleUI.ViewElements;

namespace Librarian.Views
{
    class DownloadView
    {
        private readonly View _parent;

        public string Name => _parent.Name;

        public Label Url { get; private set; }

        public Label TotalSize { get; private set; }

        public Label CurrentSize { get; private set; }

        public Label Speed { get; private set; }

        public Label TimeLeft { get; private set; }

        public Label TimeEnd { get; private set; }

        public Label Percentage { get; private set; }

        public Rectangle PercentageRect { get; private set; }

        public Label FullPercentage { get; private set; }

        public Rectangle FullPercentageRect { get; private set; }

        public Label QueueTotalSize { get; private set; }

        public Label QueueCount { get; private set; }

        public Label QueueLeft { get; private set; }

        public Label QueueEnd { get; private set; }

        public DownloadView(View view)
        {
            _parent = view;
            Initialize();
        }

        private void Initialize()
        {
            _parent.AddElement(new Rectangle(0, 0, 80, 20) { BackgroundColor = ConsoleColor.Black, Filler = ' ' });
            _parent.AddElement(new Label(0, 1, 80, 1, "Librarian - Downloads", AlignmentHorizontal.Middle, AlignmentVertical.Middle));
            _parent.AddElement(new Label(4, 4, "URL:"));
            _parent.AddElement(new Label(4, 8, "Total size:"));
            _parent.AddElement(new Label(4, 9, "Current:"));
            _parent.AddElement(new Label(4, 10, "Speed:"));
            _parent.AddElement(new Label(37, 10, "/s") { ForegroundColor = ConsoleColor.Cyan });
            _parent.AddElement(new Label(4, 11, "Eta:"));
            _parent.AddElement(new Label(35, 11, "->"));
            _parent.AddElement(new Label(4, 13, "|"));
            _parent.AddElement(new Label(67, 13, "|"));
            _parent.AddElement(new Label(74, 13, "%") { ForegroundColor = ConsoleColor.Blue });
            _parent.AddElement(new Label(4, 15, "|"));
            _parent.AddElement(new Label(67, 15, "|"));
            _parent.AddElement(new Label(74, 15, "%") { ForegroundColor = ConsoleColor.Blue });
            _parent.AddElement(new Label(4, 17, "Left in queue:"));
            _parent.AddElement(new Label(4, 18, "Eta for queue:"));
            _parent.AddElement(new Label(35, 18, "->"));

            Url = _parent.AddElement(new Label(9, 4, 66, 3, "", AlignmentHorizontal.Left, AlignmentVertical.Top, true) { ForegroundColor = ConsoleColor.DarkYellow });
            TotalSize = _parent.AddElement(new Label(16, 8, 21, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.DarkYellow });
            CurrentSize = _parent.AddElement(new Label(16, 9, 21, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Cyan });
            Speed = _parent.AddElement(new Label(16, 10, 21, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Cyan });
            TimeLeft = _parent.AddElement(new Label(16, 11, 18, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            TimeEnd = _parent.AddElement(new Label(38, 11, 37, 1, "", AlignmentHorizontal.Left, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });

            Percentage = _parent.AddElement(new Label(71, 13, 3, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            PercentageRect = _parent.AddElement(new Rectangle(5, 13, 0, 1) { Filler = ' ', BackgroundColor = ConsoleColor.Gray });

            FullPercentage = _parent.AddElement(new Label(71, 15, 3, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            FullPercentageRect = _parent.AddElement(new Rectangle(5, 15, 0, 1) { Filler = ' ', BackgroundColor = ConsoleColor.Gray });

            QueueTotalSize = _parent.AddElement(new Label(18, 17, 19, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.DarkYellow });
            QueueCount = _parent.AddElement(new Label(38, 17, 21, 1, "", AlignmentHorizontal.Left, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.DarkYellow });
            QueueLeft = _parent.AddElement(new Label(18, 18, 16, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            QueueEnd = _parent.AddElement(new Label(38, 18, 37, 1, "", AlignmentHorizontal.Left, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
        }


        public void UpdateView(DownloadState state)
        {
            Url.Text = state.Url;
            TotalSize.Text = ConvertToSizeString(state.TotalSize);
            CurrentSize.Text = ConvertToSizeString(state.CurrentSize);
            Speed.Text = ConvertToSizeString((long)state.Speed);

            Percentage.Text = ((int)(state.Completion * 100)).ToString();
            FullPercentage.Text = ((int)(state.FullQueueCompletion * 100)).ToString();
            PercentageRect.Width = (int)(state.Completion * 62);
            FullPercentageRect.Width = (int)(state.FullQueueCompletion * 62);


            QueueTotalSize.Text = state.QueueTotalSize <= 0 && state.QueuedItems > 0 ? "?" : ConvertToSizeString(state.QueueTotalSize);

            if (state.QueuedItems == 1)
                QueueCount.Text = "(1 item)";
            else
                QueueCount.Text = $"({state.QueuedItems} items)";

            if (state.SmoothSpeed <= 0)
            {
                TimeLeft.Text = "";
                TimeEnd.Text = "";
                QueueLeft.Text = "";
                QueueEnd.Text = "";
                return;
            }

            DateTime now = DateTime.Now;

            double secondsLeft = (state.TotalSize - state.CurrentSize) / state.SmoothSpeed;
            TimeLeft.Text = ConvertToTimeString(TimeSpan.FromSeconds(secondsLeft));
            TimeEnd.Text = now.AddSeconds(secondsLeft).ToString(CultureInfo.CurrentUICulture);

            if (state.QueueTotalSize <= 0)
            {
                QueueLeft.Text = state.QueuedItems > 0 ? "?" : "";
                QueueEnd.Text = state.QueuedItems > 0 ? "?" : "";
                return;
            }

            secondsLeft = state.QueueTotalSize / state.SmoothSpeed;
            QueueLeft.Text = ConvertToTimeString(TimeSpan.FromSeconds(secondsLeft));
            QueueEnd.Text = now.AddSeconds(secondsLeft).ToString(CultureInfo.CurrentUICulture);
        }

        private static string ConvertToSizeString(long bytes)
        {
            if (bytes < 1_000)
                return $"{bytes} B";

            if (bytes < 1_000_000)
                return $"{(bytes / 1_000D):n2} KB";

            if (bytes < 1_000_000_000)
                return $"{(bytes / 1_000_000D):n2} MB";

            if (bytes < 1_000_000_000_000)
                return $"{(bytes / 1_000_000_000D):n2} GB";

            return $"{(bytes / 1_000_000_000_000D):n2} TB";
        }

        private static string ConvertToTimeString(TimeSpan duration)
        {
            StringBuilder sb = new StringBuilder();

            if (duration.Days > 0)
                sb.Append(duration.Days).Append("d");
            if (duration.Hours > 0)
                sb.Append(duration.Hours).Append("h");
            if (duration.Minutes > 0)
                sb.Append(duration.Minutes).Append("m");
            if (duration.Seconds > 0)
                sb.Append(duration.Seconds).Append("s");

            return sb.ToString();
        }
    }
}
