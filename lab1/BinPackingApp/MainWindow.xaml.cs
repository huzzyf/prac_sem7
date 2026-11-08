using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GeneticBinPacking;
using SearchResult = GeneticBinPacking.SearchResult;

namespace BinPackingApp
{
    public class PartRow
    {
        public int Width { get; }

        public int Height { get; }

        public string Size => $"{Width} × {Height}";

        public string Quantity { get; set; }

        public PartRow(int width, int height, string quantity)
        {
            Width = width;
            Height = height;
            Quantity = quantity;
        }
    }

    public partial class MainWindow : Window
    {
        private const int VisualizationIntervalMilliseconds = 50;

        private readonly PartRow[] _rows =
        {
            new(1, 1, "15"),
            new(2, 2, "12"),
            new(3, 3, "8"),
            new(1, 2, "0"),
            new(2, 1, "0"),
            new(1, 3, "0"),
            new(3, 1, "0")
        };

        private readonly DispatcherTimer _timer;
        private readonly ConcurrentQueue<SearchProgress> _visualizationQueue = new();

        private CancellationTokenSource? _cancellation;
        private volatile bool _skipVisualization;
        private bool _calculationCompleted;
        private bool _closeWhenStopped;

        public MainWindow()
        {
            InitializeComponent();

            PartsList.ItemsSource = _rows;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(
                    VisualizationIntervalMilliseconds)
            };

            _timer.Tick += Timer_Tick;
        }

        private async void Start_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (_cancellation is not null)
            {
                return;
            }

            PackingProblem problem;
            GeneticOptions options;

            try
            {
                problem = ReadProblem();

                int populationSize = ParseInt(PopulationBox.Text, "Популяция");

                options = new GeneticOptions
                {
                    PopulationSize = populationSize,
                    MaxGenerations = ParseInt(GenerationsBox.Text, "Поколения"),
                    MutationRate = ParseDouble(MutationBox.Text),
                    EliteCount = Math.Clamp(populationSize / 20, 1, 4)
                };

                options.Validate();
            }
            catch (Exception exception) when (
                exception is ArgumentException or FormatException or OverflowException)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "Проверьте данные",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var cancellation = new CancellationTokenSource();

            _cancellation = cancellation;
            _calculationCompleted = false;
            _closeWhenStopped = false;
            _skipVisualization = false;

            _visualizationQueue.Clear();
            Chart.Clear();

            InputPanel.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            Scene.Solution = null;
            ResultText.Text = "Вычисление…";
            ProgressText.Text = "Поколение: 0";
            StatusText.Text =
                "Алгоритм работает без задержек. Состояния показываются последовательно.";

            _timer.Start();

            var progress = new DirectProgress(ReportProgress);

            try
            {
                SearchResult result = await Task.Run(() =>
                    new GeneticPackingSolver().Solve(
                        problem,
                        options,
                        progress,
                        cancellation.Token));

                _calculationCompleted = true;

                if (!_skipVisualization && !_visualizationQueue.IsEmpty)
                {
                    string calculationStatus = result.Cancelled
                        ? "Расчёт остановлен"
                        : "Расчёт завершён";

                    StatusText.Text =
                        $"{calculationStatus} за {result.Elapsed.TotalSeconds:F2} с. " +
                        "Продолжается воспроизведение результатов…";
                }

                while (!_skipVisualization && !_visualizationQueue.IsEmpty)
                {
                    await Task.Delay(50);
                }

                ShowResult(
                    result.Best,
                    result.CompletedGenerations,
                    result.Elapsed);

                Chart.AddSamples(new[]
                {
                    new MetricSample(result.CompletedGenerations, result.Best.Area, result.Best.Perimeter)
                });

                string reason = result.Cancelled
                    ? "Поиск остановлен."
                    : "Достигнут лимит поколений.";

                string visualizationStatus = _skipVisualization
                    ? "Оставшиеся кадры пропущены. "
                    : string.Empty;

                string qualityStatus = result.ReachedLowerBound
                    ? "Площадь оптимальна. Минимальность периметра не доказана."
                    : "Показано лучшее найденное решение; глобальный оптимум не гарантируется.";

                StatusText.Text = $"{reason} Время алгоритма: {result.Elapsed.TotalSeconds:F2} с. " +
                    visualizationStatus + qualityStatus;
            }
            catch (Exception exception)
            {
                _skipVisualization = true;
                _visualizationQueue.Clear();

                StatusText.Text = "Ошибка: " + exception.Message;

                if (!_closeWhenStopped)
                {
                    MessageBox.Show(this, exception.Message, "Ошибка вычислений", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                _timer.Stop();

                _skipVisualization = true;
                _visualizationQueue.Clear();

                _cancellation = null;
                cancellation.Dispose();

                _calculationCompleted = false;

                InputPanel.IsEnabled = true;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;

                if (_closeWhenStopped)
                {
                    Close();
                }
            }
        }

        private PackingProblem ReadProblem()
        {
            var parts = new List<Part>();

            foreach (PartRow row in _rows)
            {
                int count = ParseInt(row.Quantity, $"Количество {row.Size}");

                if (count < 0)
                {
                    throw new ArgumentException("Количество фигур не может быть отрицательным.");
                }

                if (count > PackingProblem.MaxParts - parts.Count)
                {
                    throw new ArgumentException($"Допускается не более {PackingProblem.MaxParts} фигур.");
                }

                for (int index = 0; index < count; ++index)
                {
                    parts.Add(new Part(parts.Count + 1, row.Width, row.Height));
                }
            }

            return new PackingProblem(parts);
        }

        private static int ParseInt(string text, string field)
        {
            bool parsed = int.TryParse(
                text.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value);

            if (!parsed)
            {
                throw new FormatException($"{field}: введите целое число.");
            }

            return value;
        }

        private static double ParseDouble(string text)
        {
            bool parsed = double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value);

            if (!parsed || !double.IsFinite(value))
            {
                throw new FormatException("Мутация: введите число от 0 до 1.");
            }

            return value;
        }

        private void ReportProgress(SearchProgress progress)
        {
            if (!_skipVisualization)
            {
                _visualizationQueue.Enqueue(progress);
            }
        }

        private void Timer_Tick(object? sender, EventArgs eventArgs)
        {
            FlushProgress();
        }

        private void FlushProgress()
        {
            if (_skipVisualization)
            {
                return;
            }

            if (!_visualizationQueue.TryDequeue(out SearchProgress progress))
            {
                return;
            }

            ShowResult(progress.Best, progress.Generation, progress.Elapsed);

            Chart.AddSamples(new[]
            {
                new MetricSample(progress.Generation, progress.Best.Area, progress.Best.Perimeter)
            });
        }

        private void ShowResult(PackingSolution solution, int generation, TimeSpan elapsed)
        {
            if (!ReferenceEquals(Scene.Solution, solution))
            {
                Scene.Solution = solution;
            }

            ResultText.Text =
                $"Площадь: {solution.Area}   •   " +
                $"Периметр: {solution.Perimeter}   •   " +
                $"Размер: {solution.Width} × {solution.Height}";

            ProgressText.Text =
                $"Поколение: {generation}   •   " +
                $"Заполнение: {solution.Utilization:P1}   •   " +
                $"Время алгоритма: {elapsed.TotalSeconds:F1} с";
        }

        private void Stop_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (_cancellation is null)
            {
                return;
            }

            _skipVisualization = true;
            _visualizationQueue.Clear();

            StopButton.IsEnabled = false;

            if (!_calculationCompleted)
            {
                _cancellation.Cancel();

                StatusText.Text = "Останавливаем расчёт и пропускаем оставшиеся кадры…";
            }
            else
            {
                StatusText.Text = "Воспроизведение остановлено. Показываем итоговое решение…";
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs eventArgs)
        {
            if (_cancellation is null)
            {
                return;
            }

            eventArgs.Cancel = true;
            _closeWhenStopped = true;
            _skipVisualization = true;

            _visualizationQueue.Clear();

            if (!_calculationCompleted)
            {
                _cancellation.Cancel();
            }

            StopButton.IsEnabled = false;
            StatusText.Text = "Завершаем работу…";
        }

        private class DirectProgress : IProgress<SearchProgress>
        {
            private readonly Action<SearchProgress> _callback;

            public DirectProgress(Action<SearchProgress> callback)
            {
                _callback = callback;
            }

            public void Report(SearchProgress value)
            {
                _callback(value);
            }
        }
    }
}