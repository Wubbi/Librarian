using System;
using System.Globalization;
using System.Text;
using queuepacked.ConsoleUI;
using queuepacked.ConsoleUI.ViewElements;

namespace Librarian.Views
{
    class ScanView
    {
        private readonly View _parent;

        public string Name => _parent.Name;
        
        public Label TotalVersions { get; private set; }

        public Label ValidatedVersions { get; private set; }

        public Label Speed { get; private set; }

        public Label TimeLeft { get; private set; }

        public Label TimeEnd { get; private set; }

        public Label Percentage { get; private set; }

        public Rectangle PercentageRect { get; private set; }

        public ScanView(View view)
        {
            _parent = view;
            Initialize();
        }

        private void Initialize()
        {
            _parent.AddElement(new Rectangle(0, 0, 80, 20) { BackgroundColor = ConsoleColor.Black, Filler = ' ' });
            _parent.AddElement(new Label(0, 1, 80, 1, "Librarian - Scan", AlignmentHorizontal.Middle, AlignmentVertical.Middle));
            _parent.AddElement(new Label(0, 4, 80, 1, "Validating files", AlignmentHorizontal.Middle, AlignmentVertical.Middle));
            _parent.AddElement(new Label(4, 6, "Total versions:"));
            _parent.AddElement(new Label(4, 7, "Validated:"));
            _parent.AddElement(new Label(4, 8, "Speed:"));
            _parent.AddElement(new Label(37, 8, "/s") { ForegroundColor = ConsoleColor.Cyan });
            _parent.AddElement(new Label(4, 9, "Eta:"));
            _parent.AddElement(new Label(35, 9, "->"));
            _parent.AddElement(new Label(4, 13, "|"));
            _parent.AddElement(new Label(67, 13, "|"));
            _parent.AddElement(new Label(74, 13, "%") { ForegroundColor = ConsoleColor.Blue });

            TotalVersions = _parent.AddElement(new Label(20, 6, 17, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.DarkYellow });
            ValidatedVersions = _parent.AddElement(new Label(16, 7, 21, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Cyan });
            Speed = _parent.AddElement(new Label(16, 8, 21, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Cyan });
            TimeLeft = _parent.AddElement(new Label(16, 9, 18, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            TimeEnd = _parent.AddElement(new Label(38, 9, 37, 1, "", AlignmentHorizontal.Left, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });

            Percentage = _parent.AddElement(new Label(71, 13, 3, 1, "", AlignmentHorizontal.Right, AlignmentVertical.Top) { ForegroundColor = ConsoleColor.Blue });
            PercentageRect = _parent.AddElement(new Rectangle(5, 13, 0, 1) { Filler = ' ', BackgroundColor = ConsoleColor.Gray });
        }

        public void UpdateView(ScanState state)
        {
            TotalVersions.Text = state.TotalVersions.ToString();
            ValidatedVersions.Text = state.ScannedVersions.ToString();
            Speed.Text = ((int)state.Speed).ToString();

            Percentage.Text = ((int)(state.Completion * 100)).ToString();
            PercentageRect.Width = (int)(state.Completion * 62);
            

            if (state.SmoothSpeed <= 0)
            {
                TimeLeft.Text = "";
                TimeEnd.Text = "";
                return;
            }

            DateTime now = DateTime.Now;

            double secondsLeft = (state.TotalVersions - state.ScannedVersions) / state.SmoothSpeed;
            TimeLeft.Text = ConvertToTimeString(TimeSpan.FromSeconds(secondsLeft));
            TimeEnd.Text = now.AddSeconds(secondsLeft).ToString(CultureInfo.CurrentUICulture);
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
