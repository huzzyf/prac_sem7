using System;
using System.Collections.Generic;
using System.Text;

namespace GeneticBinPacking
{
    internal class Genome
    {
        public int[] Order { get; }

        public int StripWidth { get; set; }

        public Genome(int[] order, int stripWidth)
        {
            Order = order;
            StripWidth = stripWidth;
        }

        public Genome Copy()
        {
            return new Genome((int[])Order.Clone(), StripWidth);
        }
    }


    internal static class SkylineDecoder
    {
        public static PackingSolution Decode(PackingProblem problem, Genome genome, CancellationToken cancellationToken)
        {
            int stripWidth = genome.StripWidth;

            var skyline = new int[stripWidth];
            var placements = new Placement[problem.Parts.Count];

            int width = 0;
            int height = 0;

            foreach (int index in genome.Order)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Part part = problem.Parts[index];

                int bestX = 0;
                int bestY = int.MaxValue;

                for (int x = 0; x <= stripWidth - part.Width; ++x)
                {
                    int y = 0;

                    for (int column = x; column < x + part.Width; ++column)
                    {
                        y = Math.Max(y, skyline[column]);
                    }

                    if (y < bestY)
                    {
                        bestX = x;
                        bestY = y;
                    }
                }

                int top = bestY + part.Height;

                for (int column = bestX; column < bestX + part.Width; ++column)
                {
                    skyline[column] = top;
                }

                placements[index] = new Placement(part.Id, bestX, bestY, part.Width, part.Height);

                width = Math.Max(width, bestX + part.Width);
                height = Math.Max(height, top);
            }

            return new PackingSolution( placements, width, height, problem.TotalArea, stripWidth);
        }
    }
}
