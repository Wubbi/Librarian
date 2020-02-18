using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Librarian.Core;
using queuepacked.ConsoleUI;
using queuepacked.ConsoleUI.ViewElements;

namespace Librarian.Views
{
    class MainView
    {
        private readonly View _parent;

        public string Name => _parent.Name;

        public Label ReleaseVersion { get; private set; }

        public Label SnapshotVersion { get; private set; }

        public Label ReleaseDate { get; private set; }

        public Label SnapshotDate { get; private set; }

        public Label NextCheck { get; private set; }

        public MainView(View view)
        {
            _parent = view;
            Initialize();
        }

        private void Initialize()
        {
            //static
            _parent.AddElement(new Rectangle(0, 0, 80, 20) { BackgroundColor = ConsoleColor.Black, Filler = ' ' });
            _parent.AddElement(new Label(0, 1, 80, 1, "Librarian - Main", AlignmentHorizontal.Middle, AlignmentVertical.Middle));
            _parent.AddElement(new Label(8, 5, "Latest versions:"));
            _parent.AddElement(new Label(15, 7, "Release"));
            _parent.AddElement(new Label(15, 9, "Snapshot"));
            _parent.AddElement(new Label(8, 12, "Next check:"));
            _parent.AddElement(new Label(8, 15, "Library location:"));
            _parent.AddElement(new Label(10, 16, 55, 3, Program.Settings.LibraryRoot, AlignmentHorizontal.Left, AlignmentVertical.Top, true) { ForegroundColor = ConsoleColor.DarkYellow });

            //dynamic
            ReleaseVersion = _parent.AddElement(new Label(31, 7, 15, 1, "", AlignmentHorizontal.Middle, AlignmentVertical.Middle) { ForegroundColor = ConsoleColor.DarkYellow });
            SnapshotVersion = _parent.AddElement(new Label(31, 9, 15, 1, "", AlignmentHorizontal.Middle, AlignmentVertical.Middle) { ForegroundColor = ConsoleColor.DarkYellow });
            ReleaseDate = _parent.AddElement(new Label(54, 7, 15, 1, "") { ForegroundColor = ConsoleColor.DarkYellow });
            SnapshotDate = _parent.AddElement(new Label(54, 9, 15, 1, "") { ForegroundColor = ConsoleColor.DarkYellow });
            NextCheck = _parent.AddElement(new Label(31, 12, 37, 1, "") { ForegroundColor = ConsoleColor.Blue });
        }

        public void UpdateView(State state)
        {
            Game release = state.Web.Versions.FirstOrDefault(g => g.Type == Game.BuildType.Release && g.Id == state.Web.Latest[Game.BuildType.Release]);
            ReleaseVersion.Text = release?.Id ?? "?";
            ReleaseDate.Text = release?.ReleaseTime.ToString("d", CultureInfo.CurrentUICulture) ?? "?";

            Game snapshot = state.Web.Versions.FirstOrDefault(g => g.Type == Game.BuildType.Snapshot && g.Id == state.Web.Latest[Game.BuildType.Snapshot]);
            SnapshotVersion.Text = snapshot?.Id ?? "?";
            SnapshotDate.Text = snapshot?.ReleaseTime.ToString("d", CultureInfo.CurrentUICulture) ?? "?";

            NextCheck.Text = state.NextOnlineCheck == DateTime.MinValue ? "Now" : state.NextOnlineCheck.ToString("G", CultureInfo.CurrentUICulture);
        }
    }
}
