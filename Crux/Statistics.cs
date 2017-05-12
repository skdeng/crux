using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Crux
{
    /// <summary>
    /// Track the statistics of trading strategies
    /// </summary>
    public class Statistics
    {
        public List<PortfolioSnapshot> Snapshots { get; private set; }

        public event EventHandler<PortfolioSnapshot> SnapshotEvent;

        private double? CachedSharpeRatio { get; set; }

        public Statistics()
        {
            Snapshots = new List<PortfolioSnapshot>();
            CachedSharpeRatio = null;
        }

        public double CumulativePL()
        {
            return Snapshots.Last().CumulativePL;
        }

        public void Import(string logFilename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<PortfolioSnapshot>));
            StreamReader file = new StreamReader(logFilename);
            Snapshots = (List<PortfolioSnapshot>)serializer.Deserialize(file);
            file.Close();
        }

        public void Export(string logFilename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<PortfolioSnapshot>));
            StreamWriter file = new StreamWriter(logFilename);
            serializer.Serialize(file, Snapshots);
            file.Flush();
            file.Close();
        }

        public void Clear()
        {
            Snapshots.Clear();
        }

        public double SharpeRatio()
        {
            if (CachedSharpeRatio == null)
            {
                int timeBetweenTrades = (int)(Snapshots[1].Time - Snapshots[0].Time).TotalMinutes;
                int periodsPerYear = 525600 / timeBetweenTrades;    //number of minutes in 1 year / number of minutes between each trade

                CachedSharpeRatio = SharpeRatio(periodsPerYear, Snapshots.Select(snapshot => snapshot.PL), Snapshots.Select(snapshot => snapshot.BenchmarkPL));
            }
            return CachedSharpeRatio.Value;
        }

        public void Snapshot(double fiat, double security, double securityPrice, double benchmarkPrice = 0)
        {
            if (benchmarkPrice == 0)
            {
                benchmarkPrice = securityPrice;
            }

            double portfolioValue = fiat + security * securityPrice;
            bool hasOne = Snapshots.Count > 0;
            var newSnapshot = new PortfolioSnapshot()
            {
                Time = DateTime.Now,
                Fiat = fiat,
                Security = security,
                SecurityPrice = securityPrice,
                PortfolioValue = portfolioValue,
                PL = Snapshots.Count > 0 ? portfolioValue / Snapshots.Last().PortfolioValue - 1 : 0.0,
                CumulativePL = hasOne ? portfolioValue / Snapshots.First().PortfolioValue - 1 : 0.0,
                BenchmarkPrice = benchmarkPrice,
                BenchmarkPL = hasOne ? benchmarkPrice / Snapshots.Last().BenchmarkPrice - 1 : 0.0,
                BenchmarkCumulativePL = hasOne ? benchmarkPrice / Snapshots.First().BenchmarkPrice - 1 : 0.0
            };
            Snapshots.Add(newSnapshot);
            SnapshotEvent(this, newSnapshot);
            CachedSharpeRatio = null;
        }

        public static double SharpeRatio(int periodsPerYear, IEnumerable<double> pl, IEnumerable<double> benchmarkPL)
        {
            List<double> plDif = new List<double>();
            for (int i = 0; i < pl.Count(); i++)
            {
                plDif.Add(pl.ElementAt(i) - benchmarkPL.ElementAt(i));
            }

            return Math.Sqrt(periodsPerYear) * plDif.Average() / plDif.Std();
        }
    }

    public class PortfolioSnapshot
    {
        public DateTime Time { get; set; }
        public double Fiat { get; set; }
        public double Security { get; set; }
        public double SecurityPrice { get; set; }
        public double PortfolioValue { get; set; }
        public double PL { get; set; }
        public double CumulativePL { get; set; }
        public double BenchmarkPrice { get; set; }
        public double BenchmarkPL { get; set; }
        public double BenchmarkCumulativePL { get; set; }
    }
}