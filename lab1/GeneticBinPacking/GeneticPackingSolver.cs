using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GeneticBinPacking
{
    public class GeneticPackingSolver
    {
        public SearchResult Solve(PackingProblem problem, GeneticOptions options, IProgress<SearchProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(problem);
            ArgumentNullException.ThrowIfNull(options);

            options.Validate();

            var clock = Stopwatch.StartNew();
            var random = new Random(options.Seed);

            int[] initialOrder = Enumerable
                .Range(0, problem.Parts.Count)
                .OrderByDescending(index => problem.Parts[index].Area)
                .ThenByDescending(index => problem.Parts[index].Height)
                .ToArray();

            int initialWidth = Math.Clamp((int)Math.Ceiling(Math.Sqrt(problem.TotalArea)), problem.MinStripWidth, problem.MaxStripWidth);

            PackingSolution best = SkylineDecoder.Decode(problem, new Genome(initialOrder, initialWidth), CancellationToken.None);

            int completedGenerations = 0;

            progress?.Report(new SearchProgress(0, best, best.Area, clock.Elapsed));

            if (cancellationToken.IsCancellationRequested)
            {
                return new SearchResult(best, 0, clock.Elapsed, true, best.Area == problem.TotalArea);
            }

            Genome[] population = CreatePopulation(problem, options.PopulationSize, random);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };

            try
            {
                for (int generation = 1; generation <= options.MaxGenerations; ++generation)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var solutions = new PackingSolution[population.Length];

                    Parallel.For(0, population.Length, parallelOptions, index =>
                        {
                            solutions[index] = SkylineDecoder.Decode(problem, population[index], cancellationToken); 
                        });
                    
                    /*for (int index = 0; index < population.Length; ++index)
                    {
                        solutions[index] = SkylineDecoder.Decode(problem, population[index], cancellationToken);
                    }*/
                    int[] ranking = Enumerable.Range(0, population.Length).ToArray();

                    Array.Sort(ranking, (first, second) =>
                    {
                        int comparison = Compare(solutions[first], solutions[second]);

                        return comparison != 0 ? comparison : first.CompareTo(second);
                    });

                    PackingSolution generationBest = solutions[ranking[0]];

                    if (Compare(generationBest, best) < 0)
                    {
                        best = generationBest;
                    }

                    completedGenerations = generation;

                    progress?.Report(new SearchProgress(generation, best, solutions.Average(solution => (double)solution.Area), clock.Elapsed));

                    if (generation == options.MaxGenerations)
                    {
                        break;
                    }
                    var nextPopulation = new Genome[population.Length];

                    for (int index = 0; index < options.EliteCount; ++index)
                    {
                        nextPopulation[index] = population[ranking[index]].Copy();
                    }

                    for (int index = options.EliteCount; index < nextPopulation.Length; ++index)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Genome firstParent = Select(population, solutions, options.TournamentSize, random);

                        Genome secondParent = Select(population, solutions, options.TournamentSize, random);

                        Genome child = random.NextDouble() < options.CrossoverRate ? Cross(firstParent, secondParent, random) : firstParent.Copy();

                        Mutate(child, problem, options.MutationRate, random);

                        nextPopulation[index] = child;
                    }

                    population = nextPopulation;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new SearchResult(best, completedGenerations, clock.Elapsed, true, best.Area == problem.TotalArea);
            }

            return new SearchResult(best, completedGenerations, clock.Elapsed, cancellationToken.IsCancellationRequested, best.Area == problem.TotalArea);
        }

        private static int Compare(PackingSolution first, PackingSolution second)
        {
            int areaComparison = first.Area.CompareTo(second.Area);

            return areaComparison != 0 ? areaComparison : first.Perimeter.CompareTo(second.Perimeter);
        }

        private static Genome[] CreatePopulation(PackingProblem problem, int size, Random random)
        {
            int count = problem.Parts.Count;

            int targetWidth = Math.Clamp(
                (int)Math.Ceiling(Math.Sqrt(problem.TotalArea)),
                problem.MinStripWidth,
                problem.MaxStripWidth);

            int[] sortedOrder = Enumerable
                .Range(0, count)
                .OrderByDescending(index => problem.Parts[index].Area)
                .ThenByDescending(index => problem.Parts[index].Height)
                .ToArray();

            var population = new Genome[size];

            population[0] = new Genome((int[])sortedOrder.Clone(), targetWidth);
            population[1] = new Genome((int[])sortedOrder.Clone(), problem.MinStripWidth);
            population[2] = new Genome((int[])sortedOrder.Clone(), problem.MaxStripWidth);

            for (int index = 3; index < size; ++index)
            {
                int[] order = Enumerable.Range(0, count).ToArray();

                for (int position = count - 1; position > 0; position--)
                {
                    int otherPosition = random.Next(position + 1);

                    (order[position], order[otherPosition]) =
                        (order[otherPosition], order[position]);
                }

                int width = index % 3 == 0
                    ? random.Next(problem.MinStripWidth, problem.MaxStripWidth + 1)
                    : Math.Clamp(targetWidth + random.Next(-targetWidth / 2, targetWidth / 2 + 1), problem.MinStripWidth, problem.MaxStripWidth);

                population[index] = new Genome(order, width);
            }

            return population;
        }

        private static Genome Select(Genome[] population, PackingSolution[] solutions, int tournamentSize, Random random)
        {
            int bestIndex = random.Next(population.Length);

            for (int index = 1; index < tournamentSize; ++index)
            {
                int candidateIndex = random.Next(population.Length);

                if (Compare(solutions[candidateIndex], solutions[bestIndex]) < 0)
                {
                    bestIndex = candidateIndex;
                }
            }

            return population[bestIndex];
        }

        private static Genome Cross(Genome firstParent, Genome secondParent, Random random)
        {
            int count = firstParent.Order.Length;

            int left = random.Next(count);
            int right = random.Next(count);

            if (left > right)
            {
                (left, right) = (right, left);
            }

            int[] order = Enumerable.Repeat(-1, count).ToArray();
            var used = new bool[count];

            for (int position = left; position <= right; ++position)
            {
                order[position] = firstParent.Order[position];
                used[order[position]] = true;
            }

            int writePosition = (right + 1) % count;

            for (int step = 0; step < count; ++step)
            {
                int gene = secondParent.Order[(right + 1 + step) % count];

                if (used[gene])
                {
                    continue;
                }

                order[writePosition] = gene;
                used[gene] = true;
                writePosition = (writePosition + 1) % count;
            }

            int width = random.Next(2) == 0 ? firstParent.StripWidth : secondParent.StripWidth;

            return new Genome(order, width);
        }

        private static void Mutate(Genome genome, PackingProblem problem, double mutationRate, Random random)
        {
            if (genome.Order.Length > 1 && random.NextDouble() < mutationRate)
            {
                int firstPosition = random.Next(genome.Order.Length);
                int secondPosition = random.Next(genome.Order.Length);

                if (random.Next(2) == 0)
                {
                    (genome.Order[firstPosition], genome.Order[secondPosition]) =
                        (genome.Order[secondPosition], genome.Order[firstPosition]);
                }
                else
                {
                    if (firstPosition > secondPosition)
                    {
                        (firstPosition, secondPosition) =
                            (secondPosition, firstPosition);
                    }

                    Array.Reverse(genome.Order, firstPosition, secondPosition - firstPosition + 1);
                }
            }

            if (random.NextDouble() < mutationRate)
            {
                genome.StripWidth = random.NextDouble() < 0.2
                    ? random.Next(problem.MinStripWidth, problem.MaxStripWidth + 1)
                    : Math.Clamp(genome.StripWidth + random.Next(-3, 4), problem.MinStripWidth, problem.MaxStripWidth);
            }
        }
    }

}
