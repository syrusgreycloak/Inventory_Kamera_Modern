using System.Drawing;
using System.Runtime.InteropServices;

namespace InventoryKamera
{
    internal static class RectangleExtensions
    {
        internal static Point Center(this Rectangle r) => new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom) { }

        public RECT(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left; _Top = Top; _Right = Right; _Bottom = Bottom;
        }

        public int X { get => _Left; set => _Left = value; }
        public int Y { get => _Top; set => _Top = value; }
        public int Left { get => _Left; set => _Left = value; }
        public int Top { get => _Top; set => _Top = value; }
        public int Right { get => _Right; set => _Right = value; }
        public int Bottom { get => _Bottom; set => _Bottom = value; }
        public int Height { get => _Bottom - _Top; set => _Bottom = value + _Top; }
        public int Width { get => _Right - _Left; set => _Right = value + _Left; }
        public Point Location { get => new Point(Left, Top); set { _Left = value.X; _Top = value.Y; } }
        public Size Size { get => new Size(Width, Height); set { _Right = value.Width + _Left; _Bottom = value.Height + _Top; } }

        public static implicit operator Rectangle(RECT r) => new Rectangle(r.Left, r.Top, r.Width, r.Height);
        public static implicit operator RECT(Rectangle r) => new RECT(r.Left, r.Top, r.Right, r.Bottom);
        public static bool operator ==(RECT r1, RECT r2) => r1.Equals(r2);
        public static bool operator !=(RECT r1, RECT r2) => !r1.Equals(r2);
        public override string ToString() => "{Left: " + _Left + "; Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        public override int GetHashCode() => ToString().GetHashCode();
        public bool Equals(RECT r) => r.Left == _Left && r.Top == _Top && r.Right == _Right && r.Bottom == _Bottom;
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case RECT rect: return Equals(rect);
                case Rectangle rectangle: return Equals(new RECT(rectangle));
            }
            return false;
        }
    }
}
