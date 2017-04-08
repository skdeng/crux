using System;
using System.Collections.Generic;
using System.Linq;

namespace Crux
{
    public class Benchmark
    {
        public List<double> PL;

        public List<double> Prices;

        public Benchmark()
        {
            PL = new List<double>();
        }

        public void AddPL(double pl)
        {
            PL.Add(pl);
        }

        public double SharpeRatio(int periodsPerYear)
        {
            List<double> benchmarkPL = new List<double>();
            for (int i = 0; i < Prices.Count - 1; i++)
            {
                benchmarkPL.Add(Prices[i + 1] / Prices[i]);
            }
            // Trim the first few benchmark PL
            int trimCount = PL.Count - benchmarkPL.Count;
            benchmarkPL.RemoveRange(0, trimCount);

            List<double> plDif = new List<double>();
            for (int i = 0; i < PL.Count; i++)
            {
                plDif.Add(PL[i] - benchmarkPL[i]);
            }

            return Math.Sqrt(periodsPerYear) * plDif.Average() / plDif.Std();
        }
    }
}
