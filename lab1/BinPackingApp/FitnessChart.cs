using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace BinPackingApp
{
    public readonly struct MetricSample
    {
        public int Generation { get; }

        public long Area { get; }

        public long Perimeter { get; }

        public MetricSample(int generation, long area, long perimeter)
        {
            Generation = generation;
            Area = area;
            Perimeter = perimeter;
        }
    }

    public class FitnessChart : FrameworkElement
    {
        public const int MaxSamples = 200;

        private readonly List<MetricSample> _samples = new();

        public FitnessChart()
        {
            ClipToBounds = true;

            SizeChanged += Chart_SizeChanged;
        }

        public void Clear()
        {
            _samples.Clear();

            InvalidateVisual();
        }

        public void AddSamples(IEnumerable<MetricSample> samples)
        {
            foreach (MetricSample sample in samples)
            {
                if (_samples.Count > 0 && _samples[^1].Generation == sample.Generation)
                {
                    _samples[^1] = sample;
                }
                else
                {
                    _samples.Add(sample);
                }
            }

            if (_samples.Count > MaxSamples)
            {
                _samples.RemoveRange(0, _samples.Count - MaxSamples);
            }

            InvalidateVisual();
        }

        private void Chart_SizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(
                Brushes.White,
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            if (_samples.Count == 0)
            {
                drawingContext.DrawText(
                    DrawingText.Create(
                        this,
                        "Графики появятся после запуска поиска",
                        13,
                        Brushes.SlateGray),
                    new Point(12, 12));

                return;
            }

            double panelHeight = ActualHeight / 2;

            DrawPlot(
                drawingContext,
                "Площадь",
                sample => sample.Area,
                Brushes.RoyalBlue,
                0,
                panelHeight);

            DrawPlot(
                drawingContext,
                "Периметр",
                sample => sample.Perimeter,
                Brushes.SeaGreen,
                panelHeight,
                panelHeight);
        }

        private void DrawPlot(
            DrawingContext drawingContext,
            string title,
            Func<MetricSample, long> valueSelector,
            Brush curveBrush,
            double offset,
            double panelHeight)
        {
            const double left = 58;

            double top = offset + 23;
            double width = ActualWidth - 78;
            double height = panelHeight - 47;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            double minimum = _samples.Min(valueSelector);
            double maximum = _samples.Max(valueSelector);

            if (minimum == maximum)
            {
                minimum = Math.Max(0, minimum - 1);
                maximum += 1;
            }

            int firstGeneration = _samples[0].Generation;
            int lastGeneration = _samples[^1].Generation;
            int generationSpan = Math.Max(1, lastGeneration - firstGeneration);

            long currentValue = valueSelector(_samples[^1]);

            drawingContext.DrawText(
                DrawingText.Create(
                    this,
                    $"{title}: {currentValue}",
                    12,
                    curveBrush),
                new Point(left, offset + 2));

            var gridPen = new Pen(Brushes.Gainsboro, 0.8);

            for (int index = 0; index <= 2; ++index)
            {
                double y = top + index * height / 2;
                double value = maximum - index * (maximum - minimum) / 2;

                drawingContext.DrawLine(
                    gridPen,
                    new Point(left, y),
                    new Point(left + width, y));

                drawingContext.DrawText(
                    DrawingText.Create(
                        this,
                        value.ToString("0.#", CultureInfo.CurrentCulture),
                        10,
                        Brushes.SlateGray),
                    new Point(2, y - 6));
            }

            drawingContext.DrawLine(
                new Pen(Brushes.SlateGray, 1),
                new Point(left, top),
                new Point(left, top + height));

            Point Position(MetricSample sample)
            {
                double x = left +
                    (sample.Generation - firstGeneration) * width / generationSpan;

                double y = top + height -
                    (valueSelector(sample) - minimum) * height / (maximum - minimum);

                return new Point(x, y);
            }

            var geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(Position(_samples[0]), false, false);

                for (int index = 1; index < _samples.Count; ++index)
                {
                    context.LineTo(Position(_samples[index]), true, false);
                }
            }

            geometry.Freeze();

            drawingContext.DrawGeometry(
                null,
                new Pen(curveBrush, 1.8),
                geometry);

            drawingContext.DrawEllipse(
                curveBrush,
                null,
                Position(_samples[^1]),
                2.5,
                2.5);

            drawingContext.DrawText(
                DrawingText.Create(
                    this,
                    firstGeneration.ToString(),
                    10,
                    Brushes.SlateGray),
                new Point(left, top + height + 6));

            FormattedText endLabel = DrawingText.Create(
                this,
                $"{lastGeneration} · поколение",
                10,
                Brushes.SlateGray);

            drawingContext.DrawText(
                endLabel,
                new Point(
                    Math.Max(left + 24, left + width - endLabel.Width),
                    top + height + 6));
        }
    }

}
