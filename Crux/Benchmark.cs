using System;
using System.Collections.Generic;
using System.Linq;

namespace Crux
{
    public class Benchmark
    {
        public Benchmark()
        {

        }

        public static double SharpeRatio(int periodsPerYear, List<double> pl, List<double> prices)
        {
            List<double> benchmarkPL = new List<double>();
            for (int i = 0; i < prices.Count - 1; i++)
            {
                benchmarkPL.Add(prices[i + 1] / prices[i]);
            }
            // Trim the first few benchmark PL
            int trimCount = pl.Count - benchmarkPL.Count;
            benchmarkPL.RemoveRange(0, trimCount);

            List<double> plDif = new List<double>();
            for (int i = 0; i < pl.Count; i++)
            {
                plDif.Add(pl[i] - benchmarkPL[i]);
            }

            return Math.Sqrt(periodsPerYear) * plDif.Average() / plDif.Std();
        }
    }
}
