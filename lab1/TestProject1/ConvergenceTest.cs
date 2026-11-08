using System;
using System.Collections.Generic;
using GeneticBinPacking;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    [Collection("Packing tests")]
    public class ConvergenceTests
    {
        private readonly ITestOutputHelper _output;

        public ConvergenceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MixedSample_ResidualReachesZero()
        {
            var parts = new List<Part>();

            var groups = new[]
            {
                (Width: 1, Height: 1, Count: 14),
                (Width: 2, Height: 2, Count: 85),
                (Width: 3, Height: 3, Count: 67),
                (Width: 1, Height: 2, Count: 82),
                (Width: 2, Height: 1, Count: 71),
                (Width: 1, Height: 3, Count: 54),
                (Width: 3, Height: 1, Count: 52)
            };

            foreach (var group in groups)
            {
                for (int index = 0; index < group.Count; index++)
                {
                    parts.Add(new Part(
                        parts.Count + 1,
                        group.Width,
                        group.Height));
                }
            }

            var problem = new PackingProblem(parts);

            var options = new GeneticOptions
            {
                PopulationSize = 120,
                MaxGenerations = 1000,
                EliteCount = 4,
                TournamentSize = 3,
                CrossoverRate = 0.85,
                MutationRate = 0.30,
                Seed = 42,
                MaxDegreeOfParallelism =
                    Math.Max(1, Environment.ProcessorCount - 1)
            };

            var recorder = new ResidualRecorder(problem.TotalArea);

            var result = new GeneticPackingSolver().Solve(
                problem,
                options,
                recorder);

            Assert.NotEmpty(recorder.History);

            _output.WriteLine(
                $"Количество фигур: {parts.Count}");

            _output.WriteLine(
                $"Сумма площадей фигур: {problem.TotalArea}");

            _output.WriteLine("Поколение\tПустые клетки");

            long previousResidual = long.MaxValue;

            for (int index = 0; index < recorder.History.Count; index++)
            {
                var point = recorder.History[index];

                bool shouldPrint =
                    index == 0 ||
                    point.Residual != previousResidual ||
                    point.Generation % 100 == 0 ||
                    index == recorder.History.Count - 1;

                if (shouldPrint)
                {
                    _output.WriteLine(
                        $"{point.Generation}\t{point.Residual}");
                }

                Assert.True(
                    point.Residual >= 0,
                    "Невязка не должна быть отрицательной.");

                Assert.True(
                    point.Residual <= previousResidual,
                    $"Невязка увеличилась в поколении " +
                    $"{point.Generation}.");

                previousResidual = point.Residual;
            }

            long initialResidual = recorder.History[0].Residual;
            long finalResidual = result.Best.Area - problem.TotalArea;

            _output.WriteLine(
                $"Начальная невязка: {initialResidual}");

            _output.WriteLine(
                $"Конечная невязка: {finalResidual}");

        }

        private class ResidualRecorder : IProgress<SearchProgress>
        {
            private readonly int _figuresArea;

            public List<(int Generation, long Residual)> History
            { get; } = new();

            public ResidualRecorder(int figuresArea)
            {
                _figuresArea = figuresArea;
            }

            public void Report(SearchProgress progress)
            {
                long residual = progress.Best.Area - _figuresArea;

                History.Add((
                    progress.Generation,
                    residual));
            }
        }
    }
}