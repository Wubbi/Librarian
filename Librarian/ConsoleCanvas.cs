using System;
using System.Collections.Generic;
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

        private readonly object _accessLock;

        public ConsoleCanvas(int width, int height, string title = null)
        {
            _accessLock = new object();

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
            lock (_accessLock)
            {
                if (element is null)
                    throw new ArgumentNullException(nameof(element));

                if (_elements.Any(e => e.Name == element.Name))
                    throw new ArgumentException($"Canvas already has element named \"{element.Name}\"");

                _elements.Add(element);

                element.RedrawRequired += OnRedrawRequired;

                element.InvokeRedraw(RedrawScale.Overwrite);

                return element;
            }
        }

        public CanvasElement GetElement(string name)
        {
            lock (_accessLock)
            {
                return _elements.FirstOrDefault(e => e.Name == name) ?? throw new ArgumentException($"No element named \"{name}\" found");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_accessLock)
            {
                foreach (CanvasElement element in _elements)
                    element.RedrawRequired -= OnRedrawRequired;

                _elements.Clear();

                Console.SetCursorPosition(Width - 1, _canvasBufferTop + Height - 1);
                Console.WriteLine();

                _initialSettings.ResetConsoleToSettings();

                while (Console.KeyAvailable)
                    Console.ReadKey(true);
            }
        }

        private void OnRedrawRequired(CanvasElement sender, RedrawScale scale)
        {
            lock (_accessLock)
            {
                if (scale == RedrawScale.NoRedraw)
                    return;

                if (scale == RedrawScale.Global)
                {
                    foreach (CanvasElement canvasElement in _elements.Where(e => e.RedrawScale < RedrawScale.Global))
                        canvasElement.InvokeRedraw(RedrawScale.Global);

                    return;
                }

                int startIndex = scale == RedrawScale.Erase ? 0 : _elements.IndexOf(sender) + 1;

                foreach (CanvasElement canvasElement in _elements.Skip(startIndex).Where(e => e.Visible && e.Intersects(sender)))
                    canvasElement.InvokeRedraw(RedrawScale.Overwrite);
            }
        }

        public void Redraw()
        {
            Console.Title = _title;

            lock (_accessLock)
            {
                if (_elements.Count == 0)
                    return;

                if (_elements[0].RedrawScale == RedrawScale.Global)
                {
                    string line = new string(' ', Width);
                    for (int i = 0; i < Height; ++i)
                    {
                        Console.SetCursorPosition(0, _canvasBufferTop + i);
                        Console.Write(line);
                    }
                }

                foreach (CanvasElement canvasElement in _elements.Where(e => e.RedrawScale == RedrawScale.Erase))
                    canvasElement.Clear(this);

                foreach (CanvasElement canvasElement in _elements.Where(e => e.RedrawScale == RedrawScale.Overwrite || e.RedrawScale == RedrawScale.Global))
                    canvasElement.Draw(this);
            }
        }

        public abstract class CanvasElement
        {
            private bool _visible;
            public event Action<CanvasElement, RedrawScale> RedrawRequired;

            public RedrawScale RedrawScale { get; private set; }

            public string Name { get; }

            protected int _top;
            protected int _left;
            protected int _height;
            protected int _width;

            public int Top
            {
                get => _top;
                set
                {
                    if (value == _top)
                        return;

                    _top = value;

                    InvokeRedraw(RedrawScale.Global);
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

                    InvokeRedraw(RedrawScale.Global);
                }
            }

            public int Height
            {
                get => _height;
                set
                {
                    if (value == _height)
                        return;

                    _height = value;

                    InvokeRedraw(RedrawScale.Global);
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

                    InvokeRedraw(RedrawScale.Global);
                }
            }

            public bool Visible
            {
                get => _visible;
                set
                {
                    if (_visible == value)
                        return;

                    _visible = value;

                    InvokeRedraw(_visible ? RedrawScale.Overwrite : RedrawScale.Erase);
                }
            }

            protected CanvasElement(string name)
            {
                Name = name;
                Visible = true;
            }

            public virtual void Draw(ConsoleCanvas hostCanvas)
            {
                RedrawScale = RedrawScale.NoRedraw;
            }

            public void Clear(ConsoleCanvas hostCanvas)
            {
                RedrawScale = RedrawScale.NoRedraw;

                string line = new string(' ', Width);
                for (int i = Top; i <= Top + Height - 1; ++i)
                {
                    Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + i);
                    Console.Write(line);
                }
            }

            public void InvokeRedraw(RedrawScale scale)
            {
                if (scale < RedrawScale)
                    return;

                RedrawScale = scale;
                RedrawRequired?.Invoke(this, scale);
            }

            public bool Intersects(CanvasElement other)
            {
                return Top + Height - 1 >= other.Top && Top <= other.Top + other.Height - 1 && Left + Width - 1 >= other.Left && Left <= other.Left + other.Width - 1;
            }
        }

        public class Text : CanvasElement
        {
            private string _value;
            private Alignment _alignment;
            private char _background;


            public char Background
            {
                get => _background;
                set
                {
                    if (value == _background)
                        return;

                    _background = value;

                    InvokeRedraw(RedrawScale.Overwrite);
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

                    InvokeRedraw(RedrawScale.Overwrite);
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

                    InvokeRedraw(RedrawScale.Overwrite);
                }
            }

            public Text(string name, int top, int left, string value, Alignment alignment = Alignment.Left, char background = ' ') : this(name, top, left, value?.Length ?? 0, alignment, background)
            {
                _value = value;
            }

            public Text(string name, int top, int left, int width, Alignment alignment = Alignment.Left, char background = ' ') : base(name)
            {
                _top = top;
                _left = left;
                _width = width;
                _height = 1;
                _alignment = alignment;
                _background = background;
                _value = "";
            }

            public override void Draw(ConsoleCanvas hostCanvas)
            {
                base.Draw(hostCanvas);

                if (!Visible)
                    return;

                string display;

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

                Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + Top);
                Console.Write(display);
            }
        }

        public class Rect : CanvasElement
        {
            private int _border;

            private char _filler;

            public char Filler
            {
                get => _filler;
                set
                {
                    if (value == _filler)
                        return;

                    _filler = value;

                    InvokeRedraw(RedrawScale.Overwrite);
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

                    InvokeRedraw(RedrawScale.Global);
                }
            }

            public Rect(string name, int top, int left, int height, int width, char filler, int border = 0) : base(name)
            {
                _top = top;
                _left = left;
                _height = height;
                _width = width;
                _filler = filler;
                _border = border;
            }

            public override void Draw(ConsoleCanvas hostCanvas)
            {
                base.Draw(hostCanvas);

                if (!Visible)
                    return;

                for (int i = Top; i <= Top + Height - 1; ++i)
                {
                    if (Border > 0 && i - Top > Border - 1 && Top + Height - i > Border && Width > 2 * Border)
                    {
                        string filling = new string(Filler, Border);

                        Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + i);
                        Console.Write(filling);

                        Console.SetCursorPosition(Left + Width - Border, hostCanvas._canvasBufferTop + i);
                        Console.Write(filling);
                    }
                    else
                    {
                        Console.SetCursorPosition(Left, hostCanvas._canvasBufferTop + i);
                        Console.Write(new string(Filler, Width));
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

        public enum RedrawScale
        {
            NoRedraw = 0,
            Overwrite = 1,
            Erase = 2,
            Global = 3
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
