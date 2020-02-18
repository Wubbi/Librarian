using System;
using queuepacked.ConsoleUI;
using queuepacked.ConsoleUI.ViewElements;

namespace Librarian.Views
{
    class InitView
    {
        private readonly View _parent;

        public string Name => _parent.Name;

        public Label CurrentPath { get; private set; }

        public InitView(View view)
        {
            _parent = view;
            Initialize();
        }

        public void UpdateView(string currentPath)
        {
            CurrentPath.Text = $"Scanning: {currentPath}";
        }

        private void Initialize()
        {
            //static
            _parent.AddElement(new Rectangle(0, 0, 80, 20) { BackgroundColor = ConsoleColor.Black, Filler = ' ' });
            _parent.AddElement(new Label(0, 8, 80, 1, "Initializing Librarian", AlignmentHorizontal.Middle, AlignmentVertical.Middle));
            _parent.AddElement(new Label(0, 9, 80, 1, "Please wait", AlignmentHorizontal.Middle, AlignmentVertical.Middle));

            CurrentPath = _parent.AddElement(new Label(8, 11, 60, 3) { WrapText = true, TextAlignmentHorizontal = AlignmentHorizontal.Middle });
        }
    }
}
