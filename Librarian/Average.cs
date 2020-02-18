using System.Linq;

namespace Librarian
{
    public class Average
    {
        private readonly double[] _measurements;

        private int _head;

        private int _count;

        public double Current { get; private set; }

        public Average(int size)
        {
            _measurements = new double[size];
            Reset();
        }

        public void Reset()
        {
            _head = 0;
            _count = 0;
            Current = 0;
        }

        public double Next(double value)
        {
            _measurements[_head] = value;
            _head = (_head + 1) % _measurements.Length;

            if (_count < _measurements.Length)
                ++_count;

            Current = _measurements.Take(_count).Sum() / _count;
            return Current;
        }
    }
}