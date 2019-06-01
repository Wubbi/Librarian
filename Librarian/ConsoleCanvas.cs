using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace com.github.Wubbi.Librarian
{
    public class ConsoleCanvas : IDisposable
    {
        public int Width { get; }

        public int Height { get; }

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                    return;

                _title = value;
                Console.Title = _title;
            }
        }

        private readonly List<CanvasElement> _elements;

        private readonly ConsoleSettings _initialSettings;

        private readonly int _canvasBufferTop;
        private string _title;

        private bool _disposed;

        public ConsoleCanvas(int width, int height, string title = null)
        {
            _elements = new List<CanvasElement>();
            _initialSettings = new ConsoleSettings();

            Width = width;
            Height = height;

            if (Console.BufferWidth < Width)
                Console.BufferWidth = Width;

            if (Console.BufferHeight < Height)
                Console.BufferHeight = Height;

            _canvasBufferTop = Console.CursorTop;

            Console.CursorVisible = false;

            _title = title ?? Console.Title;

            _disposed = false;

            for (int i = 0; i < height; ++i)
                Console.WriteLine();
        }

        public T RegisterElement<T>(T element) where T : CanvasElement
        {
            if (element is null)
                throw new ArgumentNullException(nameof(element));

            if (_elements.Any(e => e.Name == element.Name))
                throw new ArgumentException($"Canvas already has element named \"{element.Name}\"");

            _elements.Add(element);

            element.Draw(this);

            element.RedrawRequired += OnRedrawRequired;

            return element;
        }

        public CanvasElement GetElement(string name)
        {
            return _elements.FirstOrDefault(e => e.Name == name) ?? throw new ArgumentException($"No element named \"{name}\" found");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (CanvasElement element in _elements)
                element.RedrawRequired -= OnRedrawRequired;

            _elements.Clear();

            Console.SetCursorPosition(Width - 1, _canvasBufferTop + Height - 1);
            Console.WriteLine();

            _initialSettings.ResetConsoleToSettings();

            while (Console.KeyAvailable)
                Console.ReadKey(true);
        }

        private void OnRedrawRequired(CanvasElement sender, bool elementOnly)
        {
            foreach (CanvasElement element in _elements.OrderBy(e => e.Visible).SkipWhile(e => elementOnly && e.Name != sender.Name))
                element.Draw(this);

            Console.SetCursorPosition(Width - 1, _canvasBufferTop + Height - 1);
        }

        public void Redraw()
        {
            OnRedrawRequired(null, false);
            Console.Title = _title;
        }

        public abstract class CanvasElement
        {
            private bool _visible;
            public event Action<CanvasElement, bool> RedrawRequired;

            public string Name { get; }

            public bool Visible
            {
                get => _visible;
                set
                {
                    if (_visible == value)
                        return;

                    _visible = value;

                    InvokeRedraw(false);
                }
            }

            protected CanvasElement(string name)
            {
                Name = name;
                Visible = true;
            }

            public abstract void Draw(ConsoleCanvas hostCanvas);

            protected void InvokeRedraw(bool elementOnly)
            {
                RedrawRequired?.Invoke(this, elementOnly);
            }
        }

        public class Text : CanvasElement
        {
            private int _top;
            private int _left;
            private int _width;

            private string _value;
            private Alignment _alignment;
            private char _background;

            public int Top
            {
                get => _top;
                set
                {
                    if (value == _top)
                        return;

                    _top = value;

                    InvokeRedraw(false);
                }
            }

            public int Left
            {
                get => _left;
                set
                {
                    if (value == _left)
                        return;

                    _left = value;

                    InvokeRedraw(false);
                }
            }

            public int Width
            {
                get => _width;
                set
                {
                    if (value == _width)
                        return;

                    _width = value;

                    InvokeRedraw(false);
                }
            }

            public char Background
            {
                get => _background;
                set
                {
                    if (value == _background)
                        return;

                    _background = value;

                    InvokeRedraw(false);
                }
            }

            public string Value
            {
                get => _value;
                set
                {
                    if (value == _value)
                        return;

                    _value = value;

                    InvokeRedraw(true);
                }
            }

            public Alignment Alignment
            {
                get => _alignment;
                set
                {
                    if (value == _alignment)
                        return;

                    _alignment = value;

                    InvokeRedraw(true);
                }
            }

            public Text(string name, int top, int left, int width, Alignment alignment = Alignment.Left, char background = ' ') : base(name)
            {
                _top = top;
                _left = left;
                _width = width;
                _alignment = alignment;
                _background = background;
                _value = "";
            }

            public override void Draw(ConsoleCanvas hostCanvas)
            {
                string display;

                if (Visible)
                {
                    if (Value.Length > Width)
                    {
                        switch (Alignment)
                        {
                            case Alignment.Right:
                                display = Value.Substring(Value.Length - Width);
                                break;
                            case Alignment.Center:
                                display = Value.Substring((Value.Length - Width) / 2);
                                break;
                            default:
                                display = Value.Substring(0, Width);
                                break;
                        }
                    }
                    else if (Value.Length < Width)
                    {
                        switch (Alignment)
                        {
                            case Alignment.Right:
                                display = Value.PadLeft(Width, Background);
                                break;
                            case Alignment.Center:
                                display = Value.PadLeft(Value.Length + (Width - Value.Length) / 2, Background);
                                display = display.PadRight(Width, Background);
                                break;
                            default:
                                display = Value.PadRight(Width, Background);
                                break;
                        }
                    }
                    else
                    {
                        display = Value;
                    }
                }
                else
                {
                    display = new string(' ', Width);
                }

                Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + Top);
                Console.Write(display);
            }
        }

        public class Rect : CanvasElement
        {
            private int _top;
            private int _left;
            private int _bottom;
            private int _right;

            private int _border;

            private char _filler;

            public int Top
            {
                get => _top;
                set
                {
                    if (value == _top)
                        return;

                    _top = value;

                    InvokeRedraw(false);
                }
            }

            public int Left
            {
                get => _left;
                set
                {
                    if (value == _left)
                        return;

                    _left = value;

                    InvokeRedraw(false);
                }
            }

            public int Bottom
            {
                get => _bottom;
                set
                {
                    if (value == _bottom)
                        return;

                    _bottom = value;

                    InvokeRedraw(false);
                }
            }

            public int Right
            {
                get => _right;
                set
                {
                    if (value == _right)
                        return;

                    _right = value;

                    InvokeRedraw(false);
                }
            }

            public char Filler
            {
                get => _filler;
                set
                {
                    if (value == _filler)
                        return;

                    _filler = value;

                    InvokeRedraw(true);
                }
            }

            public int Border
            {
                get => _border;
                set
                {
                    if (value == _border)
                        return;

                    _border = value;

                    InvokeRedraw(false);
                }
            }

            public Rect(string name, int top, int left, int bottom, int right, char filler, int border = 0) : base(name)
            {
                _top = top;
                _left = left;
                _bottom = bottom;
                _right = right;
                _filler = filler;
                _border = border;
            }

            public override void Draw(ConsoleCanvas hostCanvas)
            {
                char filler = Visible ? Filler : ' ';

                for (int i = Top; i <= Bottom; ++i)
                {
                    if (Border > 0 && i - Top > Border - 1 && Bottom - i > Border - 1 && Right - Left + 1 > 2 * Border)
                    {
                        string filling = new string(filler, Border);

                        Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + i);
                        Console.Write(filling);

                        Console.SetCursorPosition(Right - Border + 1, hostCanvas._canvasBufferTop + i);
                        Console.Write(filling);
                    }
                    else
                    {
                        Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + i);
                        Console.Write(new string(filler, Right - Left + 1));
                    }
                }
            }
        }

        public enum Alignment
        {
            Left,
            Right,
            Center
        }
    }

    public class ConsoleSettings
    {
        public int CursorSize { get; }
        public ConsoleColor BackgroundColor { get; }
        public ConsoleColor ForegroundColor { get; }
        public bool CursorVisible { get; }
        public string Title { get; }
        public bool TreatControlCAsInput { get; }

        public ConsoleSettings()
        {
            CursorSize = Console.CursorSize;
            BackgroundColor = Console.BackgroundColor;
            ForegroundColor = Console.ForegroundColor;
            CursorVisible = Console.CursorVisible;
            Title = Console.Title;
            TreatControlCAsInput = Console.TreatControlCAsInput;
        }

        public void ResetConsoleToSettings()
        {
            Console.CursorSize = CursorSize;
            Console.BackgroundColor = BackgroundColor;
            Console.ForegroundColor = ForegroundColor;
            Console.CursorVisible = CursorVisible;
            Console.Title = Title;
            Console.TreatControlCAsInput = TreatControlCAsInput;
        }
    }
}
