using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace BinPackingApp
{
    internal static class DrawingText
    {
        public static FormattedText Create(Visual visual, string text, double size, Brush brush)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, brush,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
        }
    }
}
