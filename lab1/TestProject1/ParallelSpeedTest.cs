using System;
using System.Collections.Generic;
using System.Diagnostics;
using GeneticBinPacking;
using Xunit;
using Xunit.Abstractions;

namespace GoodPackingTest
{
    [Collection("Packing tests")]
    public class ParallelSpeedTests
    {
        private const int PartsCount = 3000;
        private const int PopulationSize = 96;
        private const double MinimumSeconds = 15.0;

        private readonly ITestOutputHelper _output;

        public ParallelSpeedTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [Trait("Category", "Performance")]
        public void ParallelMode_IsFasterOnHeavySample()
        {
            var parts = new List<Part>(PartsCount);
            var random = new Random(123);

            var sizes = new[]
            {
                (Width: 2, Height: 2),
                (Width: 3, Height: 3),
                (Width: 1, Height: 3),
                (Width: 3, Height: 1),
                (Width: 1, Height: 2),
                (Width: 2, Height: 1)
            };

            for (int index = 0; index < PartsCount; index++)
            {
                var size = sizes[random.Next(sizes.Length)];

                parts.Add(new Part(
                    index + 1,
                    size.Width,
                    size.Height));
            }

            var problem = new PackingProblem(parts);

            int parallelism = Environment.ProcessorCount;

            Measure(problem, 1, 2, 16);
            Measure(problem, parallelism, 2, 16);

            int generations = 24;
            double sequentialSeconds;

            while (true)
            {
                _output.WriteLine(
                    $"Однопоточный запуск: {generations} поколений.");

                sequentialSeconds = Measure(
                    problem,
                    1,
                    generations,
                    PopulationSize);

                _output.WriteLine(
                    $"Время: {sequentialSeconds:F2} с");

                if (sequentialSeconds > MinimumSeconds ||
                    generations >= 10000)
                {
                    break;
                }

                generations = Math.Min(generations * 2, 10000);
            }

            Assert.True(
                sequentialSeconds > MinimumSeconds,
                "Даже при максимальном числе поколений " +
                $"не удалось получить время больше {MinimumSeconds} с.");

            _output.WriteLine(
                $"Параллельный запуск: {generations} поколений, " +
                $"предел параллелизма {parallelism}.");

            double parallelSeconds = Measure(
                problem,
                parallelism,
                generations,
                PopulationSize);

            double speedup = sequentialSeconds / parallelSeconds;

            _output.WriteLine("");
            _output.WriteLine($"Фигур: {PartsCount}");
            _output.WriteLine($"Популяция: {PopulationSize}");
            _output.WriteLine($"Поколений: {generations}");
            _output.WriteLine($"Предел параллелизма: {parallelism}");
            _output.WriteLine(
                $"Однопоточный режим: {sequentialSeconds:F2} с");
            _output.WriteLine(
                $"Многопоточный режим: {parallelSeconds:F2} с");
            _output.WriteLine(
                $"Ускорение: {speedup:F2} раза");

            Assert.True(
                parallelSeconds < sequentialSeconds,
                $"Ускорение не получено. " +
                $"Один поток: {sequentialSeconds:F2} с; " +
                $"несколько потоков: {parallelSeconds:F2} с.");
        }

        private static double Measure(
            PackingProblem problem,
            int parallelism,
            int generations,
            int populationSize)
        {
            var options = new GeneticOptions
            {
                PopulationSize = populationSize,
                MaxGenerations = generations,
                EliteCount = 4,
                TournamentSize = 3,
                CrossoverRate = 0.85,
                MutationRate = 0.25,
                Seed = 42,
                MaxDegreeOfParallelism = parallelism
            };

            var solver = new GeneticPackingSolver();

            var stopwatch = Stopwatch.StartNew();

            solver.Solve(problem, options);

            stopwatch.Stop();

            return stopwatch.Elapsed.TotalSeconds;
        }
    }
}