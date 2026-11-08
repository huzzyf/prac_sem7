using System.Collections.ObjectModel;

namespace GeneticBinPacking
{
    public readonly struct Part
    {
        public int Id { get; }

        public int Width { get; }

        public int Height { get; }

        public int Area => Width * Height;

        public Part(int id, int width, int height)
        {
            Id = id;
            Width = width;
            Height = height;
        }
    }

    public readonly struct Placement
    {
        public int Id { get; }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public int Right => X + Width;

        public int Top => Y + Height;

        public Placement(int id, int x, int y, int width, int height)
        {
            Id = id;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public class PackingProblem
    {
        public const int MaxParts = 3000;

        public ReadOnlyCollection<Part> Parts { get; }

        public int TotalArea { get; }

        public int MinStripWidth { get; }

        public int MaxStripWidth { get; }

        public PackingProblem(IEnumerable<Part> parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            Part[] items = parts.ToArray();

            if (items.Length is < 1 or > MaxParts)
            {
                throw new ArgumentException($"Количество фигур должно быть от 1 до {MaxParts}.");
            }

            var allowed = new HashSet<(int, int)>
        {
            (1, 1),
            (2, 2),
            (3, 3),
            (1, 2),
            (2, 1),
            (1, 3),
            (3, 1)
        };

            if (items.Any(part =>
                    part.Id <= 0 ||
                    !allowed.Contains((part.Width, part.Height))))
            {
                throw new ArgumentException("Недопустимые размеры или идентификаторы фигур.");
            }

            int uniqueCount = items.Select(part => part.Id).Distinct().Count();

            if (uniqueCount != items.Length)
            {
                throw new ArgumentException(
                    "Идентификаторы фигур должны быть уникальными.");
            }

            Parts = Array.AsReadOnly(items);
            TotalArea = items.Sum(part => part.Area);
            MinStripWidth = items.Max(part => part.Width);
            MaxStripWidth = items.Sum(part => part.Width);
        }
    }

    public class PackingSolution
    {
        public ReadOnlyCollection<Placement> Placements { get; }

        public int Width { get; }

        public int Height { get; }

        public int Area => Width * Height;

        public int Perimeter => 2 * (Width + Height);

        public int UsedArea { get; }

        public double Utilization => (double)UsedArea / Area;

        public int StripWidth { get; }

        internal PackingSolution(Placement[] placements, int width, int height, int usedArea, int stripWidth)
        {
            Placements = Array.AsReadOnly(placements);
            Width = width;
            Height = height;
            UsedArea = usedArea;
            StripWidth = stripWidth;
        }
    }

    public class GeneticOptions
    {
        public int PopulationSize { get; init; } = 80;

        public int MaxGenerations { get; init; } = 300;

        public int EliteCount { get; init; } = 4;

        public int TournamentSize { get; init; } = 3;

        public double CrossoverRate { get; init; } = 0.85;

        public double MutationRate { get; init; } = 0.25;

        public int Seed { get; init; } = 42;

        public int MaxDegreeOfParallelism { get; init; } =
            Math.Max(1, Environment.ProcessorCount - 1);

        public void Validate()
        {
            if (PopulationSize is < 4 or > 2000)
            {
                throw new ArgumentException("Популяция: от 4 до 2000.");
            }

            if (MaxGenerations is < 1 or > 10000)
            {
                throw new ArgumentException("Поколения: от 1 до 10000.");
            }

            if (EliteCount < 1 || EliteCount >= PopulationSize)
            {
                throw new ArgumentException("Неверное число элитных особей.");
            }

            if (TournamentSize < 2 || TournamentSize > PopulationSize)
            {
                throw new ArgumentException("Неверный размер турнира.");
            }

            if (!double.IsFinite(CrossoverRate) || CrossoverRate is < 0 or > 1)
            {
                throw new ArgumentException("Вероятность скрещивания: от 0 до 1.");
            }

            if (!double.IsFinite(MutationRate) || MutationRate is < 0 or > 1)
            {
                throw new ArgumentException("Вероятность мутации: от 0 до 1.");
            }

            if (MaxDegreeOfParallelism < 1)
            {
                throw new ArgumentException(
                    "Степень параллелизма должна быть положительной.");
            }
        }
    }

    public class SearchProgress
    {
        public int Generation { get; }

        public PackingSolution Best { get; }

        public double AverageArea { get; }

        public TimeSpan Elapsed { get; }

        public SearchProgress(int generation, PackingSolution best, double averageArea, TimeSpan elapsed)
        {
            Generation = generation;
            Best = best;
            AverageArea = averageArea;
            Elapsed = elapsed;
        }
    }

    public class SearchResult
    {
        public PackingSolution Best { get; }

        public int CompletedGenerations { get; }

        public TimeSpan Elapsed { get; }

        public bool Cancelled { get; }

        public bool ReachedLowerBound { get; }

        public SearchResult(PackingSolution best, int completedGenerations, TimeSpan elapsed, bool cancelled, bool reachedLowerBound)
        {
            Best = best;
            CompletedGenerations = completedGenerations;
            Elapsed = elapsed;
            Cancelled = cancelled;
            ReachedLowerBound = reachedLowerBound;
        }
    }

}