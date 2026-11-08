using GeneticBinPacking;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    [Collection("Packing tests")]
    public class GoodPackingTest
    {
        private readonly ITestOutputHelper _output;

        public GoodPackingTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MixedFigures_FillRectangle6By8()
        {
            Part[] parts =
            {
                new Part(1, 3, 3),
                new Part(2, 2, 2),
                new Part(3, 1, 2),
                new Part(4, 3, 1),
                new Part(5, 1, 3),
                new Part(6, 2, 2),
                new Part(7, 2, 1),
                new Part(8, 3, 1),
                new Part(9, 1, 2),
                new Part(10, 2, 1),
                new Part(11, 1, 1),
                new Part(12, 1, 1),
                new Part(13, 2, 2),
                new Part(14, 2, 2),
                new Part(15, 2, 2)
            };

            var problem = new PackingProblem(parts);

            var options = new GeneticOptions
            {
                PopulationSize = 80,
                MaxGenerations = 500,
                EliteCount = 4,
                TournamentSize = 3,
                CrossoverRate = 0.85,
                MutationRate = 0.25,
                Seed = 42,
                MaxDegreeOfParallelism = 1
            };

            var result = new GeneticPackingSolver().Solve(
                problem,
                options);

            _output.WriteLine($"Количество фигур: {parts.Length}");
            _output.WriteLine($"Площадь фигур: {problem.TotalArea}");

            _output.WriteLine(
                $"Найденные габариты: " +
                $"{result.Best.Width} × {result.Best.Height}");

            _output.WriteLine($"Площадь: {result.Best.Area}");
            _output.WriteLine($"Периметр: {result.Best.Perimeter}");

            _output.WriteLine(
                $"Пустые клетки: " +
                $"{result.Best.Area - problem.TotalArea}");

            Assert.Equal(48L, result.Best.Area);
            Assert.Equal(28L, result.Best.Perimeter);
        }
    }
}