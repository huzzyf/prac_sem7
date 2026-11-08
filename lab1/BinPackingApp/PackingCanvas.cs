using GeneticBinPacking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace BinPackingApp
{
    public class PackingCanvas : FrameworkElement
    {
        public static readonly DependencyProperty SolutionProperty =
            DependencyProperty.Register(
                nameof(Solution),
                typeof(PackingSolution),
                typeof(PackingCanvas),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    Solution_Changed));

        private static readonly Brush[] Colors =
        {
        Brushes.LightSkyBlue,
        Brushes.LightGreen,
        Brushes.SandyBrown,
        Brushes.Plum,
        Brushes.LightCoral,
        Brushes.Turquoise,
        Brushes.Khaki
    };

        private double _scale;
        private double _left;
        private double _bottom;

        public PackingSolution? Solution
        {
            get => (PackingSolution?)GetValue(SolutionProperty);
            set => SetValue(SolutionProperty, value);
        }

        public PackingCanvas()
        {
            ClipToBounds = true;

            SizeChanged += Canvas_SizeChanged;
            MouseLeave += Canvas_MouseLeave;
        }

        private static void Solution_Changed(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs eventArgs)
        {
            var canvas = (PackingCanvas)dependencyObject;

            canvas.ToolTip = null;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            InvalidateVisual();
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs eventArgs)
        {
            ToolTip = null;
        }

        private static int ColorIndex(Placement placement)
        {
            return (placement.Width, placement.Height) switch
            {
                (1, 1) => 0,
                (2, 2) => 1,
                (3, 3) => 2,
                (1, 2) => 3,
                (2, 1) => 4,
                (1, 3) => 5,
                _ => 6
            };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(
                Brushes.White,
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            _scale = 0;

            PackingSolution? solution = Solution;

            if (solution is null)
            {
                drawingContext.DrawText(
                    DrawingText.Create(
                        this,
                        "Здесь появится размещение фигур",
                        14,
                        Brushes.SlateGray),
                    new Point(18, 18));

                return;
            }

            double availableWidth = ActualWidth - 48;
            double availableHeight = ActualHeight - 62;

            if (availableWidth <= 0 || availableHeight <= 0)
            {
                return;
            }

            _scale = Math.Min(
                availableWidth / solution.Width,
                availableHeight / solution.Height);

            _left = 24 + (availableWidth - solution.Width * _scale) / 2;
            _bottom = ActualHeight - 32 - (availableHeight - solution.Height * _scale) / 2;

            var frame = new Rect(
                _left,
                _bottom - solution.Height * _scale,
                solution.Width * _scale,
                solution.Height * _scale);

            drawingContext.DrawRectangle(Brushes.WhiteSmoke, null, frame);

            var partPen = new Pen(Brushes.DarkSlateGray, 0.8);

            foreach (Placement placement in solution.Placements)
            {
                var rectangle = new Rect(
                    _left + placement.X * _scale,
                    _bottom - placement.Top * _scale,
                    placement.Width * _scale,
                    placement.Height * _scale);

                drawingContext.DrawRectangle(
                    Colors[ColorIndex(placement)],
                    partPen,
                    rectangle);

                if (rectangle.Width > 22 && rectangle.Height > 18)
                {
                    FormattedText label = DrawingText.Create(
                        this,
                        placement.Id.ToString(),
                        Math.Min(14, _scale * 0.55),
                        Brushes.Black);

                    if (label.Width < rectangle.Width - 3)
                    {
                        var labelPosition = new Point(
                            rectangle.Left + (rectangle.Width - label.Width) / 2,
                            rectangle.Top + (rectangle.Height - label.Height) / 2);

                        drawingContext.DrawText(label, labelPosition);
                    }
                }
            }

            drawingContext.DrawRectangle(
                null,
                new Pen(Brushes.Black, 1.5),
                frame);

            drawingContext.DrawText(
                DrawingText.Create(
                    this,
                    $"Ширина: {solution.Width}",
                    12,
                    Brushes.SlateGray),
                new Point(frame.Left, _bottom + 7));

            drawingContext.DrawText(
                DrawingText.Create(
                    this,
                    $"Высота: {solution.Height}",
                    12,
                    Brushes.SlateGray),
                new Point(frame.Left, Math.Max(2, frame.Top - 22)));
        }

        protected override void OnMouseMove(MouseEventArgs eventArgs)
        {
            base.OnMouseMove(eventArgs);

            if (Solution is null || _scale <= 0)
            {
                return;
            }

            Point position = eventArgs.GetPosition(this);

            double x = (position.X - _left) / _scale;
            double y = (_bottom - position.Y) / _scale;

            string? tooltip = null;

            foreach (Placement placement in Solution.Placements)
            {
                bool inside =
                    x >= placement.X && x < placement.Right &&
                    y >= placement.Y && y < placement.Top;

                if (inside)
                {
                    tooltip =
                        $"Фигура {placement.Id}: {placement.Width} × {placement.Height}; " +
                        $"X={placement.X}, Y={placement.Y}";

                    break;
                }
            }

            if (!Equals(ToolTip, tooltip))
            {
                ToolTip = tooltip;
            }
        }
    }

}
