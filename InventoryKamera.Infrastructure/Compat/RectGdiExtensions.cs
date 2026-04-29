using System.Drawing;
using InventoryKamera; // RECT

namespace InventoryKamera.Infrastructure.Compat
{
    /// <summary>
    /// WinForms-layer interop helpers between <see cref="RECT"/> and <c>System.Drawing</c> types.
    /// These conversions were removed from <c>InventoryKamera.Core/Models/Rect.cs</c> to keep Core
    /// free of any <c>System.Drawing</c> surface (enabling future cross-platform compilation).
    /// </summary>
    public static class RectGdiExtensions
    {
        /// <summary>Converts a <see cref="RECT"/> to a <see cref="System.Drawing.Rectangle"/>.</summary>
        public static Rectangle ToDrawingRectangle(this RECT r) =>
            new Rectangle(r.Left, r.Top, r.Width, r.Height);

        /// <summary>Creates a <see cref="RECT"/> from a <see cref="System.Drawing.Rectangle"/>.</summary>
        public static RECT ToRECT(this Rectangle r) =>
            new RECT(r.Left, r.Top, r.Right, r.Bottom);

        /// <summary>Returns the top-left corner of a <see cref="RECT"/> as a <see cref="System.Drawing.Point"/>.</summary>
        public static Point GetDrawingLocation(this RECT r) => new Point(r.Left, r.Top);

        /// <summary>Returns the dimensions of a <see cref="RECT"/> as a <see cref="System.Drawing.Size"/>.</summary>
        public static Size GetDrawingSize(this RECT r) => new Size(r.Width, r.Height);

        /// <summary>Returns the center of a <see cref="System.Drawing.Rectangle"/> as a <see cref="System.Drawing.Point"/>.</summary>
        public static Point Center(this Rectangle r) => new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
    }
}

